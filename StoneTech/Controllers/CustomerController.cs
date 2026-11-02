using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoneTech.Data;
using StoneTech.Entities;
using StoneTech.Enums;
using Microsoft.AspNetCore.Authorization;
using StoneTech.Models.Customer;
using StoneTech.Services;

namespace StoneTech.Controllers
{
    // Müşteri ve borçlu müşteri işlemlerine erişebilecek roller
    [Authorize(Roles = "Admin,Manager,Sales,Accountant")]
    public class CustomerController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogService _activityLogService;

        // Veritabanı ve işlem geçmişi servisleri alınır
        public CustomerController(
            AppDbContext context,
            IActivityLogService activityLogService)
        {
            _context = context;
            _activityLogService = activityLogService;
        }

        // Müşterileri listeler
        [HttpGet]
        [Authorize(Roles = "Admin,Manager,Sales")]
        public async Task<IActionResult> Index(
            string? search,
            int page = 1,
            int pageSize = 10)
        {
            if (page < 1)
            {
                page = 1;
            }

            var allowedPageSizes = new[] { 5, 10, 20, 50 };

            if (!allowedPageSizes.Contains(pageSize))
            {
                pageSize = 10;
            }

            search = search?.Trim();

            var query = _context.Customers
                .AsNoTracking()
                .AsQueryable();

            // Ad, telefon, e-posta, T.C. kimlik no ve vergi no üzerinden arama yapar
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.CustomerName.Contains(search) ||
                    x.Phone.Contains(search) ||
                    (x.Email != null && x.Email.Contains(search)) ||
                    (x.IdentityNumber != null && x.IdentityNumber.Contains(search)) ||
                    (x.TaxNumber != null && x.TaxNumber.Contains(search)));
            }

            int totalCount = await query.CountAsync();

            int totalPages = (int)Math.Ceiling(
                totalCount / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var customers = await query
                .OrderBy(x => x.CustomerName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new CustomerListViewModel
                {
                    CustomerId = x.CustomerId,
                    CustomerType = x.CustomerType,
                    CustomerName = x.CustomerName,
                    Phone = x.Phone,
                    Email = x.Email,
                    Address = x.Address,
                    IsActive = x.IsActive,

                    // İptal ve reddedilmiş işler cari hesaba dahil edilmez
                    WorkOrderCount = x.WorkOrders.Count(w =>
                        w.Status != WorkOrderStatus.Cancelled &&
                        w.Status != WorkOrderStatus.Rejected),

                    TotalAmount = x.WorkOrders
                        .Where(w =>
                            w.Status != WorkOrderStatus.Cancelled &&
                            w.Status != WorkOrderStatus.Rejected)
                        .Sum(w => (decimal?)w.TotalAmount) ?? 0,

                    TotalPaid = x.WorkOrders
                        .Where(w =>
                            w.Status != WorkOrderStatus.Cancelled &&
                            w.Status != WorkOrderStatus.Rejected)
                        .SelectMany(w => w.Payments)
                        .Sum(p => (decimal?)p.Amount) ?? 0,

                    RemainingDebt = 0
                })
                .ToListAsync();

            foreach (var customer in customers)
            {
                customer.RemainingDebt = Math.Max(
                    0,
                    customer.TotalAmount - customer.TotalPaid);
            }

            ViewBag.Search = search ?? "";
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View(customers);
        }

        // Açık bakiyesi bulunan müşterileri ve borçlu iş emirlerini listeler
        [HttpGet]
        [Authorize(Roles = "Admin,Manager,Sales,Accountant")]
        public async Task<IActionResult> Debtors()
        {
            var customers = await _context.Customers
                .AsNoTracking()
                .Select(x => new CustomerDebtorViewModel
                {
                    CustomerId = x.CustomerId,
                    CustomerName = x.CustomerName,
                    Phone = x.Phone,

                    // İptal ve reddedilmiş işler cari hesaba dahil edilmez
                    TotalAmount = x.WorkOrders
                        .Where(w =>
                            w.Status != WorkOrderStatus.Cancelled &&
                            w.Status != WorkOrderStatus.Rejected)
                        .Sum(w => (decimal?)w.TotalAmount) ?? 0,

                    TotalPaid = x.WorkOrders
                        .Where(w =>
                            w.Status != WorkOrderStatus.Cancelled &&
                            w.Status != WorkOrderStatus.Rejected)
                        .SelectMany(w => w.Payments)
                        .Sum(p => (decimal?)p.Amount) ?? 0,

                    RemainingDebt = 0,

                    // Müşterinin cari hesaba dahil iş emirlerini getirir
                    WorkOrders = x.WorkOrders
                        .Where(w =>
                            w.Status != WorkOrderStatus.Cancelled &&
                            w.Status != WorkOrderStatus.Rejected)
                        .OrderByDescending(w => w.CreatedDate)
                        .ThenByDescending(w => w.WorkOrderId)
                        .Select(w => new CustomerDebtorWorkOrderViewModel
                        {
                            WorkOrderId = w.WorkOrderId,
                            WorkOrderNumber = w.WorkOrderNumber,
                            CreatedDate = w.CreatedDate,
                            TotalAmount = w.TotalAmount,

                            TotalPaid = w.Payments
                                .Sum(p => (decimal?)p.Amount) ?? 0,

                            RemainingAmount = 0,
                            Status = w.Status
                        })
                        .ToList()
                })
                .ToListAsync();

            // Müşteri ve iş emri bazında kalan borçları hesaplar
            foreach (var customer in customers)
            {
                customer.RemainingDebt = Math.Max(
                    0,
                    customer.TotalAmount - customer.TotalPaid);

                foreach (var workOrder in customer.WorkOrders)
                {
                    workOrder.RemainingAmount = Math.Max(
                        0,
                        workOrder.TotalAmount - workOrder.TotalPaid);
                }

                // Borcu tamamen kapanmış iş emirlerini listeden çıkarır
                customer.WorkOrders = customer.WorkOrders
                    .Where(x => x.RemainingAmount > 0)
                    .ToList();
            }

            // Yalnızca açık bakiyesi bulunan müşterileri gösterir
            customers = customers
                .Where(x =>
                    x.RemainingDebt > 0 &&
                    x.WorkOrders.Any())
                .OrderByDescending(x => x.RemainingDebt)
                .ThenBy(x => x.CustomerName)
                .ToList();

            return View(customers);
        }

        // Yeni müşteri ekleme ekranını açar
        [HttpGet]
        [Authorize(Roles = "Admin,Manager,Sales")]
        public IActionResult Create()
        {
            return View(new CustomerCreateViewModel
            {
                CustomerType = CustomerType.Individual
            });
        }

        // Yeni müşteriyi veritabanına kaydeder
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager,Sales")]
        public async Task<IActionResult> Create(
            CustomerCreateViewModel model)
        {
            NormalizeCustomerModel(model);
            ValidateCustomerType(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string customerName = model.CustomerName.Trim();
            string phone = model.Phone.Trim();

            // T.C. kimlik numarası ve vergi numarası sistem genelinde benzersiz tutulur
            if (model.CustomerType == CustomerType.Individual &&
                !string.IsNullOrWhiteSpace(model.IdentityNumber))
            {
                bool identityExists = await _context.Customers
                    .AnyAsync(x => x.IdentityNumber == model.IdentityNumber);

                if (identityExists)
                {
                    ModelState.AddModelError(
                        nameof(model.IdentityNumber),
                        "Bu T.C. kimlik numarasıyla kayıtlı bir müşteri zaten bulunuyor.");
                }
            }

            if (model.CustomerType == CustomerType.Corporate &&
                !string.IsNullOrWhiteSpace(model.TaxNumber))
            {
                bool taxNumberExists = await _context.Customers
                    .AnyAsync(x => x.TaxNumber == model.TaxNumber);

                if (taxNumberExists)
                {
                    ModelState.AddModelError(
                        nameof(model.TaxNumber),
                        "Bu vergi numarasıyla kayıtlı bir müşteri zaten bulunuyor.");
                }
            }

            // Aynı ad ve telefonla yanlışlıkla ikinci kayıt oluşturulmasını engeller
            bool customerExists = await _context.Customers
                .AnyAsync(x =>
                    x.CustomerName == customerName &&
                    x.Phone == phone);

            if (customerExists)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Aynı ad ve telefon numarasına sahip bir müşteri zaten kayıtlı.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var customer = new Customer
            {
                CustomerType = model.CustomerType,
                CustomerName = customerName,

                // Müşteri tipine ait olmayan kimlik/vergi alanları temiz tutulur
                IdentityNumber = model.CustomerType == CustomerType.Individual
                    ? model.IdentityNumber
                    : null,

                TaxNumber = model.CustomerType == CustomerType.Corporate
                    ? model.TaxNumber
                    : null,

                TaxOffice = model.CustomerType == CustomerType.Corporate
                    ? model.TaxOffice
                    : null,

                Phone = phone,
                Email = model.Email,
                Address = model.Address,
                Note = model.Note,
                IsActive = true
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            await _activityLogService.LogAsync(
                "Customer",
                "Create",
                customer.CustomerId,
                $"{customer.CustomerName} adlı müşteri oluşturuldu.");

            TempData["SuccessMessage"] =
                "Müşteri başarıyla eklendi.";

            return RedirectToAction(nameof(Index));
        }

        // Müşteri düzenleme ekranını açar
        [HttpGet]
        [Authorize(Roles = "Admin,Manager,Sales")]
        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _context.Customers
                .AsNoTracking()
                .Where(x => x.CustomerId == id)
                .Select(x => new CustomerEditViewModel
                {
                    CustomerId = x.CustomerId,
                    CustomerType = x.CustomerType,
                    CustomerName = x.CustomerName,
                    IdentityNumber = x.IdentityNumber,
                    TaxNumber = x.TaxNumber,
                    TaxOffice = x.TaxOffice,
                    Phone = x.Phone,
                    Email = x.Email,
                    Address = x.Address,
                    Note = x.Note,
                    IsActive = x.IsActive
                })
                .FirstOrDefaultAsync();

            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // Düzenlenen müşteri bilgilerini kaydeder
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager,Sales")]
        public async Task<IActionResult> Edit(
            int id,
            CustomerEditViewModel model)
        {
            if (id != model.CustomerId)
            {
                return NotFound();
            }

            NormalizeCustomerModel(model);
            ValidateCustomerType(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(x => x.CustomerId == id);

            if (customer == null)
            {
                return NotFound();
            }

            string customerName = model.CustomerName.Trim();
            string phone = model.Phone.Trim();

            if (model.CustomerType == CustomerType.Individual &&
                !string.IsNullOrWhiteSpace(model.IdentityNumber))
            {
                bool identityExists = await _context.Customers
                    .AnyAsync(x =>
                        x.CustomerId != model.CustomerId &&
                        x.IdentityNumber == model.IdentityNumber);

                if (identityExists)
                {
                    ModelState.AddModelError(
                        nameof(model.IdentityNumber),
                        "Bu T.C. kimlik numarası başka bir müşteride kayıtlı.");
                }
            }

            if (model.CustomerType == CustomerType.Corporate &&
                !string.IsNullOrWhiteSpace(model.TaxNumber))
            {
                bool taxNumberExists = await _context.Customers
                    .AnyAsync(x =>
                        x.CustomerId != model.CustomerId &&
                        x.TaxNumber == model.TaxNumber);

                if (taxNumberExists)
                {
                    ModelState.AddModelError(
                        nameof(model.TaxNumber),
                        "Bu vergi numarası başka bir müşteride kayıtlı.");
                }
            }

            bool duplicateCustomer = await _context.Customers
                .AnyAsync(x =>
                    x.CustomerId != model.CustomerId &&
                    x.CustomerName == customerName &&
                    x.Phone == phone);

            if (duplicateCustomer)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Aynı ad ve telefon numarasına sahip başka bir müşteri zaten kayıtlı.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            customer.CustomerType = model.CustomerType;
            customer.CustomerName = customerName;

            customer.IdentityNumber =
                model.CustomerType == CustomerType.Individual
                    ? model.IdentityNumber
                    : null;

            customer.TaxNumber =
                model.CustomerType == CustomerType.Corporate
                    ? model.TaxNumber
                    : null;

            customer.TaxOffice =
                model.CustomerType == CustomerType.Corporate
                    ? model.TaxOffice
                    : null;

            customer.Phone = phone;
            customer.Email = model.Email;
            customer.Address = model.Address;
            customer.Note = model.Note;
            customer.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            await _activityLogService.LogAsync(
                "Customer",
                "Update",
                customer.CustomerId,
                $"{customer.CustomerName} adlı müşterinin bilgileri güncellendi.");

            TempData["SuccessMessage"] =
                "Müşteri bilgileri başarıyla güncellendi.";

            return RedirectToAction(
                nameof(Details),
                new { id = customer.CustomerId });
        }

        // Müşterinin bilgilerini, işlerini ve cari durumunu gösterir
        [HttpGet]
        [Authorize(Roles = "Admin,Manager,Sales")]
        public async Task<IActionResult> Details(int id)
        {
            var customer = await _context.Customers
                .AsNoTracking()
                .Where(x => x.CustomerId == id)
                .Select(x => new CustomerDetailsViewModel
                {
                    CustomerId = x.CustomerId,
                    CustomerType = x.CustomerType,
                    CustomerName = x.CustomerName,
                    IdentityNumber = x.IdentityNumber,
                    TaxNumber = x.TaxNumber,
                    TaxOffice = x.TaxOffice,
                    Phone = x.Phone,
                    Email = x.Email,
                    Address = x.Address,
                    Note = x.Note,
                    IsActive = x.IsActive,

                    // İptal ve reddedilmiş işler cari toplamların dışında tutulur
                    WorkOrderCount = x.WorkOrders.Count(w =>
                        w.Status != WorkOrderStatus.Cancelled &&
                        w.Status != WorkOrderStatus.Rejected),

                    TotalAmount = x.WorkOrders
                        .Where(w =>
                            w.Status != WorkOrderStatus.Cancelled &&
                            w.Status != WorkOrderStatus.Rejected)
                        .Sum(w => (decimal?)w.TotalAmount) ?? 0,

                    TotalPaid = x.WorkOrders
                        .Where(w =>
                            w.Status != WorkOrderStatus.Cancelled &&
                            w.Status != WorkOrderStatus.Rejected)
                        .SelectMany(w => w.Payments)
                        .Sum(p => (decimal?)p.Amount) ?? 0,

                    RemainingDebt = 0,

                    WorkOrders = x.WorkOrders
                        .OrderByDescending(w => w.CreatedDate)
                        .ThenByDescending(w => w.WorkOrderId)
                        .Select(w => new CustomerWorkOrderViewModel
                        {
                            WorkOrderId = w.WorkOrderId,
                            WorkOrderNumber = w.WorkOrderNumber,
                            CreatedDate = w.CreatedDate,
                            TotalAmount = w.TotalAmount,

                            TotalPaid = w.Payments
                                .Sum(p => (decimal?)p.Amount) ?? 0,

                            RemainingAmount = 0,
                            Status = w.Status,
                            Description = w.Description
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (customer == null)
            {
                return NotFound();
            }

            customer.RemainingDebt = Math.Max(
                0,
                customer.TotalAmount - customer.TotalPaid);

            foreach (var workOrder in customer.WorkOrders)
            {
                // İptal ve reddedilmiş işlerde cari borç gösterilmez
                if (workOrder.Status == WorkOrderStatus.Cancelled ||
                    workOrder.Status == WorkOrderStatus.Rejected)
                {
                    workOrder.RemainingAmount = 0;
                    continue;
                }

                workOrder.RemainingAmount = Math.Max(
                    0,
                    workOrder.TotalAmount - workOrder.TotalPaid);
            }

            return View(customer);
        }

        // Aktif müşteriyi pasif duruma getirir
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager,Sales")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(x => x.CustomerId == id);

            if (customer == null)
            {
                return NotFound();
            }

            if (!customer.IsActive)
            {
                return RedirectToAction(nameof(Index));
            }

            customer.IsActive = false;
            await _context.SaveChangesAsync();

            await _activityLogService.LogAsync(
                "Customer",
                "StatusChange",
                customer.CustomerId,
                $"{customer.CustomerName} adlı müşteri pasif duruma getirildi.");

            TempData["SuccessMessage"] =
                "Müşteri pasif duruma getirildi.";

            return RedirectToAction(nameof(Index));
        }

        // Pasif müşteriyi tekrar aktif duruma getirir
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager,Sales")]
        public async Task<IActionResult> Activate(int id)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(x => x.CustomerId == id);

            if (customer == null)
            {
                return NotFound();
            }

            if (customer.IsActive)
            {
                return RedirectToAction(nameof(Index));
            }

            customer.IsActive = true;
            await _context.SaveChangesAsync();

            await _activityLogService.LogAsync(
                "Customer",
                "StatusChange",
                customer.CustomerId,
                $"{customer.CustomerName} adlı müşteri tekrar aktif duruma getirildi.");

            TempData["SuccessMessage"] =
                "Müşteri tekrar aktif duruma getirildi.";

            return RedirectToAction(nameof(Index));
        }

        // Create modelindeki metin alanlarını standart biçimde temizler
        private static void NormalizeCustomerModel(
            CustomerCreateViewModel model)
        {
            model.CustomerName = model.CustomerName?.Trim() ?? string.Empty;
            model.Phone = model.Phone?.Trim() ?? string.Empty;
            model.IdentityNumber = NormalizeNullable(model.IdentityNumber);
            model.TaxNumber = NormalizeNullable(model.TaxNumber);
            model.TaxOffice = NormalizeNullable(model.TaxOffice);
            model.Email = NormalizeNullable(model.Email);
            model.Address = NormalizeNullable(model.Address);
            model.Note = NormalizeNullable(model.Note);
        }

        // Edit modelindeki metin alanlarını standart biçimde temizler
        private static void NormalizeCustomerModel(
            CustomerEditViewModel model)
        {
            model.CustomerName = model.CustomerName?.Trim() ?? string.Empty;
            model.Phone = model.Phone?.Trim() ?? string.Empty;
            model.IdentityNumber = NormalizeNullable(model.IdentityNumber);
            model.TaxNumber = NormalizeNullable(model.TaxNumber);
            model.TaxOffice = NormalizeNullable(model.TaxOffice);
            model.Email = NormalizeNullable(model.Email);
            model.Address = NormalizeNullable(model.Address);
            model.Note = NormalizeNullable(model.Note);
        }

        // Bireysel ve kurumsal müşteri alanlarını iş kuralına göre doğrular
        private void ValidateCustomerType(
            CustomerCreateViewModel model)
        {
            if (!Enum.IsDefined(typeof(CustomerType), model.CustomerType))
            {
                ModelState.AddModelError(
                    nameof(model.CustomerType),
                    "Geçerli bir müşteri tipi seçiniz.");

                return;
            }

            if (model.CustomerType == CustomerType.Individual)
            {
                if (string.IsNullOrWhiteSpace(model.IdentityNumber))
                {
                    ModelState.AddModelError(
                        nameof(model.IdentityNumber),
                        "Bireysel müşteri için T.C. kimlik numarası zorunludur.");
                }

                model.TaxNumber = null;
                model.TaxOffice = null;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(model.TaxNumber))
                {
                    ModelState.AddModelError(
                        nameof(model.TaxNumber),
                        "Kurumsal müşteri için vergi numarası zorunludur.");
                }

                if (string.IsNullOrWhiteSpace(model.TaxOffice))
                {
                    ModelState.AddModelError(
                        nameof(model.TaxOffice),
                        "Kurumsal müşteri için vergi dairesi zorunludur.");
                }

                model.IdentityNumber = null;
            }
        }

        // Bireysel ve kurumsal müşteri alanlarını iş kuralına göre doğrular
        private void ValidateCustomerType(
            CustomerEditViewModel model)
        {
            if (!Enum.IsDefined(typeof(CustomerType), model.CustomerType))
            {
                ModelState.AddModelError(
                    nameof(model.CustomerType),
                    "Geçerli bir müşteri tipi seçiniz.");

                return;
            }

            if (model.CustomerType == CustomerType.Individual)
            {
                if (string.IsNullOrWhiteSpace(model.IdentityNumber))
                {
                    ModelState.AddModelError(
                        nameof(model.IdentityNumber),
                        "Bireysel müşteri için T.C. kimlik numarası zorunludur.");
                }

                model.TaxNumber = null;
                model.TaxOffice = null;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(model.TaxNumber))
                {
                    ModelState.AddModelError(
                        nameof(model.TaxNumber),
                        "Kurumsal müşteri için vergi numarası zorunludur.");
                }

                if (string.IsNullOrWhiteSpace(model.TaxOffice))
                {
                    ModelState.AddModelError(
                        nameof(model.TaxOffice),
                        "Kurumsal müşteri için vergi dairesi zorunludur.");
                }

                model.IdentityNumber = null;
            }
        }

        // Boş metinleri null, dolu metinleri kırpılmış biçimde döndürür
        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}