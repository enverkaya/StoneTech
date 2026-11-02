using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.EntityFrameworkCore;
using StoneTech.Data;
using StoneTech.Entities;
using StoneTech.Enums;
using StoneTech.Models.Payment;
using StoneTech.Services;

namespace StoneTech.Controllers
{
    // Ödeme modülüne Admin, Manager, Sales ve Accountant rolleri erişebilir
    [Authorize(Roles = "Admin,Manager,Sales,Accountant")]
    public class PaymentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogService _activityLogService;

        // Veritabanı ve işlem geçmişi servislerini controller'a alır
        public PaymentController(
            AppDbContext context,
            IActivityLogService activityLogService)
        {
            _context = context;
            _activityLogService = activityLogService;
        }

        // İş emirlerini ödeme durumlarıyla birlikte listeler
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            int page = 1,
            int pageSize = 10)
        {
            // Geçersiz sayfa numarasını düzeltir
            if (page < 1)
            {
                page = 1;
            }

            // Kullanılmasına izin verilen sayfa boyutları
            int[] allowedPageSizes = { 5, 10, 20, 50 };

            if (!allowedPageSizes.Contains(pageSize))
            {
                pageSize = 10;
            }

            // Arama metnini temizler
            search = search?.Trim();

            // İptal veya reddedilmemiş iş emirlerini sorgular
            var query = _context.WorkOrders
                .AsNoTracking()
                .Where(x =>
                    x.Status != WorkOrderStatus.Cancelled &&
                    x.Status != WorkOrderStatus.Rejected)
                .AsQueryable();

            // İş emri numarası, müşteri adı veya telefonuna göre arama yapar
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.WorkOrderNumber.Contains(search) ||
                    x.Customer.CustomerName.Contains(search) ||
                    x.Customer.Phone.Contains(search));
            }

            // Toplam iş emri sayısını hesaplar
            int totalCount = await query.CountAsync();

            // Toplam sayfa sayısını hesaplar
            int totalPages = (int)Math.Ceiling(
                totalCount / (double)pageSize);

            // Mevcut olmayan sayfa istenirse son sayfaya çeker
            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            // İş emirlerini ödeme bilgileriyle birlikte getirir
            var payments = await query
                .OrderByDescending(x => x.CreatedDate)
                .ThenByDescending(x => x.WorkOrderId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new PaymentListViewModel
                {
                    // Veritabanı kimliği yönlendirmelerde kullanılmaya devam eder
                    WorkOrderId = x.WorkOrderId,

                    // Kullanıcıya gösterilecek iş emri numarası
                    WorkOrderNumber = x.WorkOrderNumber,

                    CustomerId = x.CustomerId,
                    CustomerName = x.Customer.CustomerName,
                    WorkOrderDate = x.CreatedDate,
                    TotalAmount = x.TotalAmount,

                    TotalPaid = x.Payments
                        .Select(p => (decimal?)p.Amount)
                        .Sum() ?? 0,

                    RemainingAmount =
                        x.TotalAmount -
                        (x.Payments
                            .Select(p => (decimal?)p.Amount)
                            .Sum() ?? 0),

                    PaymentCount = x.Payments.Count
                })
                .ToListAsync();

            // Arama ve sayfalama bilgilerini View'a gönderir
            ViewBag.Search = search ?? "";
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View(payments);
        }

        // Yeni ödeme alma ekranını açar
        [HttpGet]
        public async Task<IActionResult> Create(int workOrderId)
        {
            // İş emrini müşteri ve ödemeleriyle birlikte getirir
            var workOrder = await _context.WorkOrders
                .AsNoTracking()
                .Include(x => x.Customer)
                .Include(x => x.Payments)
                .FirstOrDefaultAsync(x =>
                    x.WorkOrderId == workOrderId);

            if (workOrder == null)
            {
                return NotFound();
            }

            // İptal veya reddedilmiş iş emrine ödeme alınmasını engeller
            if (workOrder.Status == WorkOrderStatus.Cancelled ||
                workOrder.Status == WorkOrderStatus.Rejected)
            {
                TempData["ErrorMessage"] =
                    "İptal veya reddedilmiş iş emrine ödeme alınamaz.";

                // Muhasebeci iş emri detayına erişemediği için borçlular ekranına döner
                if (User.IsInRole("Accountant"))
                {
                    return RedirectToAction("Debtors", "Customer");
                }

                return RedirectToAction(
                    "Details",
                    "WorkOrder",
                    new { id = workOrderId });
            }

            // Daha önce yapılan toplam ödemeyi hesaplar
            decimal totalPaid = workOrder.Payments
                .Sum(x => x.Amount);

            // Kalan borcu hesaplar
            decimal remainingAmount =
                workOrder.TotalAmount - totalPaid;

            // Borcu tamamen kapanmış iş emrine yeni ödeme alınmasını engeller
            if (remainingAmount <= 0)
            {
                TempData["ErrorMessage"] =
                    "Bu iş emrinin kalan borcu bulunmuyor.";

                // Muhasebeci iş emri detayına erişemediği için borçlular ekranına döner
                if (User.IsInRole("Accountant"))
                {
                    return RedirectToAction("Debtors", "Customer");
                }

                return RedirectToAction(
                    "Details",
                    "WorkOrder",
                    new { id = workOrderId });
            }

            // Ödeme ekranında kullanılacak modeli hazırlar
            var model = new PaymentCreateViewModel
            {
                // Veritabanı kimliği form işlemlerinde kullanılır
                WorkOrderId = workOrder.WorkOrderId,

                // Kullanıcıya gösterilecek iş emri numarası
                WorkOrderNumber = workOrder.WorkOrderNumber,

                CustomerName = workOrder.Customer.CustomerName,
                WorkOrderTotal = workOrder.TotalAmount,
                TotalPaid = totalPaid,
                RemainingAmount = remainingAmount,
                PaymentDate = DateTime.Today,
                PaymentMethod = PaymentMethod.Cash
            };

            return View(model);
        }

        // Yeni ödemeyi kaydeder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            PaymentCreateViewModel model)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            // İş emrini müşteri bilgisiyle getirir
            var workOrder = await _context.WorkOrders
                .Include(x => x.Customer)
                .FirstOrDefaultAsync(x =>
                    x.WorkOrderId == model.WorkOrderId);

            if (workOrder == null)
            {
                return NotFound();
            }

            // İptal veya reddedilmiş iş emrine ödeme alınmasını engeller
            if (workOrder.Status == WorkOrderStatus.Cancelled ||
                workOrder.Status == WorkOrderStatus.Rejected)
            {
                await transaction.RollbackAsync();

                TempData["ErrorMessage"] =
                    "İptal veya reddedilmiş iş emrine ödeme alınamaz.";

                // Muhasebeci iş emri detayına erişemediği için borçlular ekranına döner
                if (User.IsInRole("Accountant"))
                {
                    return RedirectToAction("Debtors", "Customer");
                }

                return RedirectToAction(
                    "Details",
                    "WorkOrder",
                    new { id = model.WorkOrderId });
            }

            // Veritabanındaki gerçek toplam ödemeyi hesaplar
            decimal totalPaid = await _context.Payments
                .Where(x =>
                    x.WorkOrderId == model.WorkOrderId)
                .Select(x => (decimal?)x.Amount)
                .SumAsync() ?? 0;

            // Veritabanındaki gerçek kalan borcu hesaplar
            decimal remainingAmount =
                workOrder.TotalAmount - totalPaid;

            // Kullanıcı tarafından değiştirilemeyecek ekran bilgilerini
            // veritabanındaki gerçek değerlerle tekrar doldurur
            model.WorkOrderNumber =
                workOrder.WorkOrderNumber;

            model.CustomerName =
                workOrder.Customer.CustomerName;

            model.WorkOrderTotal =
                workOrder.TotalAmount;

            model.TotalPaid =
                totalPaid;

            model.RemainingAmount =
                remainingAmount;

            // Sıfır veya negatif ödeme yapılmasını engeller
            if (model.Amount <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.Amount),
                    "Ödeme tutarı 0'dan büyük olmalıdır.");
            }

            // Borcu kapanmış iş emrine ödeme alınmasını engeller
            if (remainingAmount <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.Amount),
                    "Bu iş emrinin kalan borcu bulunmuyor.");
            }

            // Kalan borçtan fazla ödeme yapılmasını engeller
            if (model.Amount > remainingAmount)
            {
                ModelState.AddModelError(
                    nameof(model.Amount),
                    $"Ödeme tutarı kalan borçtan fazla olamaz. " +
                    $"Kalan borç: {remainingAmount:N2} ₺.");
            }

            // Geçerli ödeme yöntemlerini kontrol eder
            if (model.PaymentMethod != PaymentMethod.Cash &&
                model.PaymentMethod != PaymentMethod.CreditCard &&
                model.PaymentMethod != PaymentMethod.BankTransfer)
            {
                ModelState.AddModelError(
                    nameof(model.PaymentMethod),
                    "Geçerli bir ödeme yöntemi seçiniz.");
            }

            // Doğrulama hatası varsa ödeme ekranını tekrar gösterir
            if (!ModelState.IsValid)
            {
                await transaction.RollbackAsync();
                return View(model);
            }

            // Yeni ödeme kaydını oluşturur
            var payment = new Payment
            {
                WorkOrderId = model.WorkOrderId,
                Amount = model.Amount,
                PaymentDate = model.PaymentDate,
                PaymentMethod = model.PaymentMethod,
                Description =
                    string.IsNullOrWhiteSpace(model.Description)
                        ? null
                        : model.Description.Trim()
            };

            try
            {
                // Ödemeyi veritabanına ekler
                _context.Payments.Add(payment);

                await _context.SaveChangesAsync();

                // Başarılı ödeme işlemini işlem geçmişine kaydeder
                await _activityLogService.LogAsync(
                    "Payment",
                    "PaymentReceived",
                    payment.PaymentId,
                    $"{workOrder.WorkOrderNumber} numaralı iş emri için " +
                    $"{workOrder.Customer.CustomerName} müşterisinden " +
                    $"{payment.Amount:N2} ₺ ödeme alındı.");

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "Ödeme kaydedilemedi. Borç bilgisi değişmiş olabilir; lütfen tekrar deneyin.");

                return View(model);
            }

            TempData["SuccessMessage"] =
                $"{model.Amount:N2} ₺ ödeme başarıyla alındı.";

            // Muhasebeci ödeme sonrasında borçlu müşteriler ekranına döner
            if (User.IsInRole("Accountant"))
            {
                return RedirectToAction("Debtors", "Customer");
            }

            // Diğer yetkili roller iş emri detayına döner
            return RedirectToAction(
                "Details",
                "WorkOrder",
                new { id = model.WorkOrderId });
        }

        // Ödeme detayını gösterir
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            // Ödemeyi iş emri ve müşteri bilgileriyle getirir
            var payment = await _context.Payments
                .AsNoTracking()
                .Include(x => x.WorkOrder)
                    .ThenInclude(x => x.Customer)
                .FirstOrDefaultAsync(x =>
                    x.PaymentId == id);

            if (payment == null)
            {
                return NotFound();
            }

            // İş emrine yapılan toplam ödemeyi hesaplar
            decimal totalPaid = await _context.Payments
                .AsNoTracking()
                .Where(x =>
                    x.WorkOrderId == payment.WorkOrderId)
                .Select(x => (decimal?)x.Amount)
                .SumAsync() ?? 0;

            // Ödeme detay modelini hazırlar
            var model = new PaymentDetailsViewModel
            {
                PaymentId = payment.PaymentId,

                // Veritabanı kimliği yönlendirmelerde kullanılır
                WorkOrderId = payment.WorkOrderId,

                // Kullanıcıya gösterilecek iş emri numarası
                WorkOrderNumber =
                    payment.WorkOrder.WorkOrderNumber,

                CustomerId =
                    payment.WorkOrder.CustomerId,

                CustomerName =
                    payment.WorkOrder.Customer.CustomerName,

                Amount = payment.Amount,
                PaymentDate = payment.PaymentDate,
                PaymentMethod = payment.PaymentMethod,
                Description = payment.Description,

                WorkOrderTotal =
                    payment.WorkOrder.TotalAmount,

                TotalPaid = totalPaid,

                RemainingAmount =
                    payment.WorkOrder.TotalAmount -
                    totalPaid
            };

            return View(model);
        }
    }
}
