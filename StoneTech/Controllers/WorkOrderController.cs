using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.EntityFrameworkCore;
using StoneTech.Data;
using StoneTech.Entities;
using StoneTech.Enums;
using StoneTech.Models.WorkOrder;
using StoneTech.Services;

namespace StoneTech.Controllers
{
    // İş emri modülüne Admin, Manager, Sales ve Workshop rolleri erişebilir
    [Authorize(Roles = "Admin,Manager,Sales,Workshop")]
    public class WorkOrderController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogService _activityLogService;

        // Veritabanı bağlantısını ve işlem geçmişi servisini controller'a alır
        public WorkOrderController(
            AppDbContext context,
            IActivityLogService activityLogService)
        {
            _context = context;
            _activityLogService = activityLogService;
        }

        // İş emirlerini listeler
        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
        {
            if (page < 1)
                page = 1;

            int[] allowedPageSizes = { 5, 10, 20, 50 };

            if (!allowedPageSizes.Contains(pageSize))
                pageSize = 10;

            var query = _context.WorkOrders
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x => x.WorkOrderNumber.Contains(search) || x.Customer.CustomerName.Contains(search) || x.Customer.Phone.Contains(search));
            }

            int totalCount = await query.CountAsync();

            int totalPages = (int)Math.Ceiling(
                totalCount / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
                page = totalPages;

            var workOrders = await query
                .OrderByDescending(x => x.CreatedDate)
                .ThenByDescending(x => x.WorkOrderId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new WorkOrderListViewModel
                {
                    WorkOrderId = x.WorkOrderId,
                    WorkOrderNumber = x.WorkOrderNumber,
                    CustomerName = x.Customer.CustomerName,
                    CreatedDate = x.CreatedDate,
                    Status = x.Status,
                    TotalAmount = x.TotalAmount,

                    TotalPaid = x.Payments
                        .Select(p => (decimal?)p.Amount)
                        .Sum() ?? 0,

                    RemainingAmount =
                        x.Status == WorkOrderStatus.Cancelled ||
                        x.Status == WorkOrderStatus.Rejected
                            ? 0
                            : Math.Max(
                                0,
                                x.TotalAmount -
                                (x.Payments
                                    .Select(p => (decimal?)p.Amount)
                                    .Sum() ?? 0))
                })
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View(workOrders);
        }

        // Yeni iş emri oluşturma ekranını açar
        [Authorize(Roles = "Admin,Manager,Sales")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadCreateDataAsync();

            var createdDate = DateTime.Today;

            var model = new WorkOrderCreateViewModel
            {
                // Sıradaki iş emri numarasını ekranda otomatik gösterir
                WorkOrderNumber =
                    await GenerateWorkOrderNumberAsync(createdDate),

                CreatedDate = createdDate,
                VatRate = 20m,
                Details = new List<WorkOrderDetailCreateViewModel>
        {
            new WorkOrderDetailCreateViewModel()
        }
            };

            return View(model);
        }

        // Yeni iş emrini kaydeder
        [Authorize(Roles = "Admin,Manager,Sales")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WorkOrderCreateViewModel model)
        {
            model.Details ??= new List<WorkOrderDetailCreateViewModel>();

            model.Details = model.Details
                .Where(x =>
                    x.StoneId > 0 ||
                    x.Quantity > 0 ||
                    x.UnitPrice > 0 ||
                    !string.IsNullOrWhiteSpace(x.Description))
                .ToList();

            bool customerExists = await _context.Customers
                .AnyAsync(x =>
                    x.CustomerId == model.CustomerId &&
                    x.IsActive);

            if (!customerExists)
            {
                ModelState.AddModelError(
                    nameof(model.CustomerId),
                    "Seçilen müşteri bulunamadı veya aktif değil.");
            }

            if (model.Details.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(model.Details),
                    "İş emrine en az bir taş eklemelisiniz.");
            }

            var duplicateStoneIds = model.Details
                .Where(x => x.StoneId > 0)
                .GroupBy(x => x.StoneId)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();

            if (duplicateStoneIds.Count > 0)
            {
                ModelState.AddModelError(
                    nameof(model.Details),
                    "Aynı taş iş emrine birden fazla kez eklenemez.");
            }

            if (!ModelState.IsValid)
            {
                // Hata durumunda iş emri numarasını ekranda tekrar gösterir
                model.WorkOrderNumber =
                    await GenerateWorkOrderNumberAsync(model.CreatedDate);

                await LoadCreateDataAsync();

                return View(model);
            }

            var stoneIds = model.Details
                .Select(x => x.StoneId)
                .Distinct()
                .ToList();

            int validStoneCount = await _context.Stones
                .AsNoTracking()
                .CountAsync(x =>
                    stoneIds.Contains(x.StoneId) &&
                    x.IsActive);

            if (validStoneCount != stoneIds.Count)
            {
                ModelState.AddModelError(
                    nameof(model.Details),
                    "Seçilen taşlardan biri bulunamadı veya aktif değil.");

                // Hata durumunda iş emri numarasını ekranda tekrar gösterir
                model.WorkOrderNumber =
                    await GenerateWorkOrderNumberAsync(model.CreatedDate);

                await LoadCreateDataAsync();

                return View(model);
            }

            CalculateTotals(
                model.Details.Select(x =>
                    new PriceLine(
                        x.Quantity,
                        x.UnitPrice)),
                model.CuttingMeter,
                model.CuttingPricePerMeter,
                model.VatRate,
                out decimal subTotal,
                out decimal vatAmount,
                out decimal totalAmount);

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                // Kaydetme anında sıradaki iş emri numarasını sistem yeniden oluşturur
                string workOrderNumber =
                    await GenerateWorkOrderNumberAsync(model.CreatedDate);

                var workOrder = new WorkOrder
                {
                    WorkOrderNumber = workOrderNumber,
                    CustomerId = model.CustomerId,
                    CreatedDate = model.CreatedDate,
                    CuttingMeter = model.CuttingMeter,
                    CuttingPricePerMeter = model.CuttingPricePerMeter,
                    SubTotal = subTotal,
                    VatRate = model.VatRate,
                    VatAmount = vatAmount,
                    TotalAmount = totalAmount,
                    Status = WorkOrderStatus.PendingApproval,
                    Description = NormalizeNullable(model.Description)
                };

                _context.WorkOrders.Add(workOrder);

                await _context.SaveChangesAsync();

                foreach (var detail in model.Details)
                {
                    _context.WorkOrderDetails.Add(
                        new WorkOrderDetail
                        {
                            WorkOrderId = workOrder.WorkOrderId,
                            StoneId = detail.StoneId,
                            Quantity = detail.Quantity,
                            UnitPrice = detail.UnitPrice,
                            Description = NormalizeNullable(detail.Description)
                        });
                }

                // Create aşamasında stok düşülmez
                await _context.SaveChangesAsync();

                // Oluşturulan iş emrini işlem geçmişine kaydeder
                await _activityLogService.LogAsync(
                    "WorkOrder",
                    "Create",
                    workOrder.WorkOrderId,
                    $"İş emri {workOrder.WorkOrderNumber} oluşturuldu ve onay bekliyor.");

                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    $"İş emri {workOrder.WorkOrderNumber} oluşturuldu ve onay bekliyor.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = workOrder.WorkOrderId
                    });
            }
            catch
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "İş emri kaydedilirken bir hata oluştu.");

                // Kayıt başarısız olursa sıradaki numarayı ekranda tekrar gösterir
                model.WorkOrderNumber =
                    await GenerateWorkOrderNumberAsync(model.CreatedDate);

                await LoadCreateDataAsync();

                return View(model);
            }
        }

        // İş emrinin detaylarını gösterir
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var workOrder = await _context.WorkOrders
                .AsNoTracking()
                .Include(x => x.Customer)
                .Include(x => x.WorkOrderDetails)
                .ThenInclude(x => x.Stone)
                .Include(x => x.Payments)
                .FirstOrDefaultAsync(x => x.WorkOrderId == id);

            if (workOrder == null)
                return NotFound();

            decimal stoneTotal = workOrder.WorkOrderDetails
                .Sum(x => x.Quantity * x.UnitPrice);

            decimal cuttingTotal =
                workOrder.CuttingMeter *
                workOrder.CuttingPricePerMeter;

            decimal totalPaid = workOrder.Payments.Sum(x => x.Amount);

            decimal remainingAmount =
                workOrder.Status == WorkOrderStatus.Cancelled ||
                workOrder.Status == WorkOrderStatus.Rejected
                    ? 0
                    : Math.Max(0, workOrder.TotalAmount - totalPaid);

            var model = new WorkOrderDetailsViewModel
            {
                WorkOrderId = workOrder.WorkOrderId,
                WorkOrderNumber = workOrder.WorkOrderNumber,
                CustomerId = workOrder.CustomerId,
                CustomerName = workOrder.Customer.CustomerName,
                Phone = workOrder.Customer.Phone,
                CreatedDate = workOrder.CreatedDate,
                Status = workOrder.Status,
                Description = workOrder.Description,
                CuttingMeter = workOrder.CuttingMeter,
                CuttingPricePerMeter = workOrder.CuttingPricePerMeter,
                CuttingTotal = cuttingTotal,
                StoneTotal = stoneTotal,

                SubTotal = workOrder.SubTotal,
                VatRate = workOrder.VatRate,
                VatAmount = workOrder.VatAmount,
                TotalAmount = workOrder.TotalAmount,

                TotalPaid = totalPaid,
                RemainingAmount = remainingAmount,

                Details = workOrder.WorkOrderDetails
                    .Select(x => new WorkOrderDetailViewModel
                    {
                        WorkOrderDetailId = x.WorkOrderDetailId,
                        StoneId = x.StoneId,
                        StoneName = x.Stone.StoneName,
                        Quantity = x.Quantity,
                        UnitPrice = x.UnitPrice,
                        LineTotal = x.Quantity * x.UnitPrice,
                        Description = x.Description
                    })
                    .ToList(),

                Payments = workOrder.Payments
                    .OrderByDescending(x => x.PaymentDate)
                    .ThenByDescending(x => x.PaymentId)
                    .Select(x => new WorkOrderPaymentViewModel
                    {
                        PaymentId = x.PaymentId,
                        Amount = x.Amount,
                        PaymentDate = x.PaymentDate,
                        PaymentMethod = x.PaymentMethod,
                        Description = x.Description
                    })
                    .ToList()
            };

            return View(model);
        }

        // İş emri düzenleme ekranını açar
        [Authorize(Roles = "Admin,Manager,Sales")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var workOrder = await _context.WorkOrders
                .AsNoTracking()
                .Include(x => x.WorkOrderDetails)
                .FirstOrDefaultAsync(x => x.WorkOrderId == id);

            if (workOrder == null)
                return NotFound();

            if (workOrder.Status != WorkOrderStatus.PendingApproval)
            {
                TempData["ErrorMessage"] =
                    "İş emri yalnızca onay beklerken düzenlenebilir.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            await LoadEditDataAsync(id);

            var model = new WorkOrderEditViewModel
            {
                WorkOrderId = workOrder.WorkOrderId,
                CustomerId = workOrder.CustomerId,
                CreatedDate = workOrder.CreatedDate,
                CuttingMeter = workOrder.CuttingMeter,
                CuttingPricePerMeter = workOrder.CuttingPricePerMeter,
                VatRate = workOrder.VatRate,
                Status = workOrder.Status,
                Description = workOrder.Description,

                Details = workOrder.WorkOrderDetails
                    .Select(x => new WorkOrderDetailEditViewModel
                    {
                        WorkOrderDetailId = x.WorkOrderDetailId,
                        StoneId = x.StoneId,
                        Quantity = x.Quantity,
                        UnitPrice = x.UnitPrice,
                        Description = x.Description
                    })
                    .ToList()
            };

            return View(model);
        }

        // Düzenlenen iş emrini kaydeder
        [Authorize(Roles = "Admin,Manager,Sales")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, WorkOrderEditViewModel model)
        {
            if (id != model.WorkOrderId)
                return NotFound();

            model.Details ??= new List<WorkOrderDetailEditViewModel>();

            model.Details = model.Details
                .Where(x =>
                    x.StoneId > 0 ||
                    x.Quantity > 0 ||
                    x.UnitPrice > 0 ||
                    !string.IsNullOrWhiteSpace(x.Description))
                .ToList();

            // Düzenleme ile onay/ödeme işlemlerinin aynı anda eski veriye göre
            // karar vermesini engeller.
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            var existingWorkOrder = await _context.WorkOrders
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.WorkOrderId == id);

            if (existingWorkOrder == null)
                return NotFound();

            if (existingWorkOrder.Status != WorkOrderStatus.PendingApproval)
            {
                TempData["ErrorMessage"] =
                    "İş emri yalnızca onay beklerken düzenlenebilir.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            // Durum formdan değiştirilemez.
            model.Status = existingWorkOrder.Status;

            bool customerExists = await _context.Customers
                .AnyAsync(x =>
                    x.CustomerId == model.CustomerId &&
                    x.IsActive);

            if (!customerExists)
            {
                ModelState.AddModelError(
                    nameof(model.CustomerId),
                    "Seçilen müşteri bulunamadı veya aktif değil.");
            }

            if (model.Details.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(model.Details),
                    "İş emrine en az bir taş eklemelisiniz.");
            }

            var duplicateStoneIds = model.Details
                .Where(x => x.StoneId > 0)
                .GroupBy(x => x.StoneId)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();

            if (duplicateStoneIds.Count > 0)
            {
                ModelState.AddModelError(
                    nameof(model.Details),
                    "Aynı taş iş emrine birden fazla kez eklenemez.");
            }

            if (!ModelState.IsValid)
            {
                await LoadEditDataAsync(id);
                return View(model);
            }

            var stoneIds = model.Details
                .Select(x => x.StoneId)
                .Distinct()
                .ToList();

            var existingStoneIds = await _context.WorkOrderDetails
                .AsNoTracking()
                .Where(x => x.WorkOrderId == id)
                .Select(x => x.StoneId)
                .Distinct()
                .ToListAsync();

            int validStoneCount = await _context.Stones
                .AsNoTracking()
                .CountAsync(x =>
                    stoneIds.Contains(x.StoneId) &&
                    (x.IsActive || existingStoneIds.Contains(x.StoneId)));

            if (validStoneCount != stoneIds.Count)
            {
                ModelState.AddModelError(
                    nameof(model.Details),
                    "Seçilen taşlardan biri bulunamadı veya kullanılamıyor.");

                await LoadEditDataAsync(id);
                return View(model);
            }

            CalculateTotals(
                model.Details.Select(x => new PriceLine(x.Quantity, x.UnitPrice)),
                model.CuttingMeter,
                model.CuttingPricePerMeter,
                model.VatRate,
                out decimal subTotal,
                out decimal vatAmount,
                out decimal totalAmount);

            decimal totalPaid = await _context.Payments
                .Where(x => x.WorkOrderId == id)
                .Select(x => (decimal?)x.Amount)
                .SumAsync() ?? 0;

            if (totalAmount < totalPaid)
            {
                ModelState.AddModelError(
                    string.Empty,
                    $"İş emrinin yeni toplamı yapılan ödemelerden düşük olamaz. " +
                    $"Toplam ödeme: {totalPaid:N2} ₺.");

                await LoadEditDataAsync(id);
                return View(model);
            }

            try
            {
                var workOrder = await _context.WorkOrders
                    .FirstOrDefaultAsync(x => x.WorkOrderId == id);

                if (workOrder == null)
                {
                    await transaction.RollbackAsync();
                    return NotFound();
                }

                if (workOrder.Status != WorkOrderStatus.PendingApproval)
                {
                    await transaction.RollbackAsync();

                    TempData["ErrorMessage"] =
                        "İş emrinin durumu değiştiği için düzenleme kaydedilemedi.";

                    return RedirectToAction(
                        nameof(Details),
                        new { id });
                }

                var oldDetails = await _context.WorkOrderDetails
                    .Where(x => x.WorkOrderId == id)
                    .ToListAsync();

                _context.WorkOrderDetails.RemoveRange(oldDetails);

                workOrder.CustomerId = model.CustomerId;
                workOrder.CreatedDate = model.CreatedDate;
                workOrder.CuttingMeter = model.CuttingMeter;
                workOrder.CuttingPricePerMeter = model.CuttingPricePerMeter;
                workOrder.SubTotal = subTotal;
                workOrder.VatRate = model.VatRate;
                workOrder.VatAmount = vatAmount;
                workOrder.TotalAmount = totalAmount;
                workOrder.Description = NormalizeNullable(model.Description);

                await _context.SaveChangesAsync();

                foreach (var detail in model.Details)
                {
                    _context.WorkOrderDetails.Add(new WorkOrderDetail
                    {
                        WorkOrderId = workOrder.WorkOrderId,
                        StoneId = detail.StoneId,
                        Quantity = detail.Quantity,
                        UnitPrice = detail.UnitPrice,
                        Description = NormalizeNullable(detail.Description)
                    });
                }

                // PendingApproval aşamasında stok hareketi oluşturulmaz.
                await _context.SaveChangesAsync();

                // Güncellenen iş emrini işlem geçmişine kaydeder
                await _activityLogService.LogAsync(
                    "WorkOrder",
                    "Update",
                    workOrder.WorkOrderId,
                    $"İş emri {workOrder.WorkOrderNumber} güncellendi.");

                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    $"İş emri {workOrder.WorkOrderNumber} başarıyla güncellendi.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = workOrder.WorkOrderId });
            }
            catch
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "İş emri güncellenirken bir hata oluştu.");

                await LoadEditDataAsync(id);
                return View(model);
            }
        }

        // Onay bekleyen iş emrini onaylar
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            var workOrder = await _context.WorkOrders
                .FirstOrDefaultAsync(x => x.WorkOrderId == id);

            if (workOrder == null)
                return NotFound();

            if (workOrder.Status != WorkOrderStatus.PendingApproval)
            {
                TempData["ErrorMessage"] =
                    "Yalnızca onay bekleyen iş emirleri onaylanabilir.";

                return RedirectToAction(nameof(Details), new { id });
            }

            workOrder.Status = WorkOrderStatus.Approved;

            // İş emri onayını işlem geçmişine kaydeder
            await _activityLogService.LogAsync(
                "WorkOrder",
                "Approve",
                workOrder.WorkOrderId,
                $"İş emri {workOrder.WorkOrderNumber} onaylandı.");

            await transaction.CommitAsync();

            TempData["SuccessMessage"] =
                $"İş emri {workOrder.WorkOrderNumber} onaylandı.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // Onay bekleyen iş emrini reddeder
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            var workOrder = await _context.WorkOrders
                .Include(x => x.Payments)
                .FirstOrDefaultAsync(x => x.WorkOrderId == id);

            if (workOrder == null)
                return NotFound();

            if (workOrder.Status != WorkOrderStatus.PendingApproval)
            {
                TempData["ErrorMessage"] =
                    "Yalnızca onay bekleyen iş emirleri reddedilebilir.";

                return RedirectToAction(nameof(Details), new { id });
            }

            if (workOrder.Payments.Any())
            {
                TempData["ErrorMessage"] =
                    "Ödeme alınmış iş emri reddedilemez.";

                return RedirectToAction(nameof(Details), new { id });
            }

            workOrder.Status = WorkOrderStatus.Rejected;

            // İş emri reddini işlem geçmişine kaydeder
            await _activityLogService.LogAsync(
                "WorkOrder",
                "Reject",
                workOrder.WorkOrderId,
                $"İş emri {workOrder.WorkOrderNumber} reddedildi.");

            await transaction.CommitAsync();

            TempData["SuccessMessage"] =
                $"İş emri {workOrder.WorkOrderNumber} reddedildi.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // Onaylanmış iş emrini atölyede işleme alır ve stok çıkışını yapar
        [Authorize(Roles = "Admin,Manager,Workshop")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartWorkshop(int id)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var workOrder = await _context.WorkOrders
                    .Include(x => x.WorkOrderDetails)
                        .ThenInclude(x => x.Stone)
                    .FirstOrDefaultAsync(x => x.WorkOrderId == id);

                if (workOrder == null)
                {
                    await transaction.RollbackAsync();
                    return NotFound();
                }

                if (workOrder.Status != WorkOrderStatus.Approved)
                {
                    await transaction.RollbackAsync();

                    TempData["ErrorMessage"] =
                        "Yalnızca onaylanmış iş emirleri atölyede işleme alınabilir.";

                    return RedirectToAction(nameof(Details), new { id });
                }

                if (workOrder.WorkOrderDetails.Count == 0)
                {
                    await transaction.RollbackAsync();

                    TempData["ErrorMessage"] =
                        "Taş detayı bulunmayan iş emri atölyede işleme alınamaz.";

                    return RedirectToAction(nameof(Details), new { id });
                }

                bool alreadyExited = await _context.StockMovements
                    .AnyAsync(x =>
                        x.WorkOrderId == id &&
                        x.MovementType == StockMovementType.Exit);

                if (alreadyExited)
                {
                    await transaction.RollbackAsync();

                    TempData["ErrorMessage"] =
                        "Bu iş emri için stok çıkışı daha önce yapılmış.";

                    return RedirectToAction(nameof(Details), new { id });
                }

                var stoneIds = workOrder.WorkOrderDetails
                    .Select(x => x.StoneId)
                    .Distinct()
                    .ToList();

                var currentStocks =
                    await GetCurrentStocksAsync(stoneIds);

                foreach (var detail in workOrder.WorkOrderDetails)
                {
                    int currentStock =
                        currentStocks.GetValueOrDefault(
                            detail.StoneId,
                            0);

                    if (detail.Quantity > currentStock)
                    {
                        await transaction.RollbackAsync();

                        TempData["ErrorMessage"] =
                            $"{detail.Stone.StoneName} için yeterli stok yok. " +
                            $"Mevcut stok: {currentStock} plaka, " +
                            $"gereken: {detail.Quantity} plaka.";

                        return RedirectToAction(nameof(Details), new { id });
                    }
                }

                foreach (var detail in workOrder.WorkOrderDetails)
                {
                    _context.StockMovements.Add(new StockMovement
                    {
                        StoneId = detail.StoneId,
                        Quantity = detail.Quantity,
                        MovementType = StockMovementType.Exit,
                        MovementDate = DateTime.Now,
                        Description =
                            $"İş emri {workOrder.WorkOrderNumber} atölye stok çıkışı",
                        PurchaseId = null,
                        WorkOrderId = workOrder.WorkOrderId
                    });
                }

                workOrder.Status = WorkOrderStatus.InWorkshop;

                await _context.SaveChangesAsync();

                // Atölyede işleme alınan iş emrini işlem geçmişine kaydeder
                await _activityLogService.LogAsync(
                    "WorkOrder",
                    "StartWorkshop",
                    workOrder.WorkOrderId,
                    $"İş emri {workOrder.WorkOrderNumber} atölyede işleme alındı ve plakalar stoktan düşüldü.");

                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    $"İş emri {workOrder.WorkOrderNumber} atölyede işleme alındı ve plakalar stoktan düşüldü.";
            }
            catch
            {
                await transaction.RollbackAsync();

                TempData["ErrorMessage"] =
                    "İş emri atölyede işleme alınırken bir hata oluştu.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // Atölyede işlemde olan iş emrini tamamlar
        [Authorize(Roles = "Admin,Manager,Workshop")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            var workOrder = await _context.WorkOrders
                .FirstOrDefaultAsync(x => x.WorkOrderId == id);

            if (workOrder == null)
                return NotFound();

            if (workOrder.Status != WorkOrderStatus.InWorkshop)
            {
                TempData["ErrorMessage"] =
                    "Yalnızca atölyede işlemde olan iş emirleri tamamlanabilir.";

                return RedirectToAction(nameof(Details), new { id });
            }

            workOrder.Status = WorkOrderStatus.Completed;

            // Tamamlanan iş emrini işlem geçmişine kaydeder
            await _activityLogService.LogAsync(
                "WorkOrder",
                "Complete",
                workOrder.WorkOrderId,
                $"İş emri {workOrder.WorkOrderNumber} tamamlandı.");

            await transaction.CommitAsync();

            TempData["SuccessMessage"] =
                $"İş emri {workOrder.WorkOrderNumber} tamamlandı.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // İş emrini iş akışı kurallarına göre iptal eder
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var workOrder = await _context.WorkOrders
                    .Include(x => x.Payments)
                    .FirstOrDefaultAsync(x => x.WorkOrderId == id);

                if (workOrder == null)
                {
                    await transaction.RollbackAsync();
                    return NotFound();
                }

                if (workOrder.Status == WorkOrderStatus.Cancelled)
                {
                    await transaction.RollbackAsync();

                    TempData["ErrorMessage"] =
                        $"İş emri {workOrder.WorkOrderNumber} zaten iptal edilmiş.";

                    return RedirectToAction(nameof(Details), new { id });
                }

                if (workOrder.Status == WorkOrderStatus.Rejected)
                {
                    await transaction.RollbackAsync();

                    TempData["ErrorMessage"] =
                        "Reddedilmiş iş emri iptal edilemez.";

                    return RedirectToAction(nameof(Details), new { id });
                }

                if (workOrder.Status == WorkOrderStatus.Completed)
                {
                    await transaction.RollbackAsync();

                    TempData["ErrorMessage"] =
                        "Tamamlanmış iş emri doğrudan iptal edilemez.";

                    return RedirectToAction(nameof(Details), new { id });
                }

                if (workOrder.Payments.Any())
                {
                    await transaction.RollbackAsync();

                    TempData["ErrorMessage"] =
                        "Bu iş emrine ödeme alındığı için doğrudan iptal edilemez.";

                    return RedirectToAction(nameof(Details), new { id });
                }

                if (workOrder.Status == WorkOrderStatus.PendingApproval ||
                    workOrder.Status == WorkOrderStatus.Approved)
                {
                    // Bu aşamalarda stok çıkışı yapılmadığı için iade oluşturulmaz.
                    workOrder.Status = WorkOrderStatus.Cancelled;

                    await _context.SaveChangesAsync();

                    // İş emri iptalini işlem geçmişine kaydeder
                    await _activityLogService.LogAsync(
                        "WorkOrder",
                        "Cancel",
                        workOrder.WorkOrderId,
                        $"İş emri {workOrder.WorkOrderNumber} iptal edildi.");

                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] =
                        $"İş emri {workOrder.WorkOrderNumber} iptal edildi.";

                    return RedirectToAction(nameof(Details), new { id });
                }

                if (workOrder.Status != WorkOrderStatus.InWorkshop)
                {
                    await transaction.RollbackAsync();

                    TempData["ErrorMessage"] =
                        "İş emrinin mevcut durumu iptal işlemine uygun değil.";

                    return RedirectToAction(nameof(Details), new { id });
                }

                var exitMovements = await _context.StockMovements
                    .Where(x =>
                        x.WorkOrderId == id &&
                        x.MovementType == StockMovementType.Exit)
                    .GroupBy(x => x.StoneId)
                    .Select(x => new
                    {
                        StoneId = x.Key,
                        Quantity = x.Sum(y => y.Quantity)
                    })
                    .ToListAsync();

                if (exitMovements.Count == 0)
                {
                    await transaction.RollbackAsync();

                    TempData["ErrorMessage"] =
                        "Atölyede olan iş emrine ait stok çıkışı bulunamadığı için iptal işlemi tamamlanamadı.";

                    return RedirectToAction(nameof(Details), new { id });
                }

                bool hasReturnMovement = await _context.StockMovements
                    .AnyAsync(x =>
                        x.WorkOrderId == id &&
                        x.MovementType == StockMovementType.WorkOrderReturn);

                if (hasReturnMovement)
                {
                    await transaction.RollbackAsync();

                    TempData["ErrorMessage"] =
                        "Bu iş emrinin stok iadesi daha önce yapılmış.";

                    return RedirectToAction(nameof(Details), new { id });
                }

                foreach (var exitMovement in exitMovements)
                {
                    _context.StockMovements.Add(new StockMovement
                    {
                        StoneId = exitMovement.StoneId,
                        Quantity = exitMovement.Quantity,
                        MovementType = StockMovementType.WorkOrderReturn,
                        MovementDate = DateTime.Now,
                        Description =
                            $"İş emri {workOrder.WorkOrderNumber} iptal iadesi",
                        PurchaseId = null,
                        WorkOrderId = workOrder.WorkOrderId
                    });
                }

                workOrder.Status = WorkOrderStatus.Cancelled;

                await _context.SaveChangesAsync();

                // Atölyedeki iş emri iptalini ve stok iadesini işlem geçmişine kaydeder
                await _activityLogService.LogAsync(
                    "WorkOrder",
                    "Cancel",
                    workOrder.WorkOrderId,
                    $"İş emri {workOrder.WorkOrderNumber} iptal edildi ve atölyeye verilen plakalar stoğa geri eklendi.");

                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    $"İş emri {workOrder.WorkOrderNumber} iptal edildi ve atölyeye verilen plakalar stoğa geri eklendi.";

                return RedirectToAction(nameof(Details), new { id });
            }
            catch
            {
                await transaction.RollbackAsync();

                TempData["ErrorMessage"] =
                    "İş emri iptal edilirken bir hata oluştu.";

                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // Create ekranında kullanılacak müşteri ve taş listelerini hazırlar
        private async Task LoadCreateDataAsync()
        {
            ViewBag.Customers = await _context.Customers
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.CustomerName)
                .Select(x => new
                {
                    x.CustomerId,
                    x.CustomerName,
                    x.Phone
                })
                .ToListAsync();

            ViewBag.Stones = await _context.Stones
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.StoneName)
                .Select(x => new
                {
                    x.StoneId,
                    x.StoneName,
                    x.SalePrice,

                    CurrentStock = x.StockMovements
                        .Select(m =>
                            m.MovementType == StockMovementType.Entry ||
                            m.MovementType == StockMovementType.AdjustmentIn ||
                            m.MovementType == StockMovementType.WorkOrderReturn
                                ? m.Quantity
                                : -m.Quantity)
                        .Sum()
                })
                .ToListAsync();
        }

        // Edit ekranında kullanılacak müşteri ve taş listelerini hazırlar
        private async Task LoadEditDataAsync(int workOrderId)
        {
            int currentCustomerId = await _context.WorkOrders
                .Where(x => x.WorkOrderId == workOrderId)
                .Select(x => x.CustomerId)
                .FirstOrDefaultAsync();

            ViewBag.Customers = await _context.Customers
                .AsNoTracking()
                .Where(x =>
                    x.IsActive ||
                    x.CustomerId == currentCustomerId)
                .OrderBy(x => x.CustomerName)
                .Select(x => new
                {
                    x.CustomerId,
                    x.CustomerName,
                    x.Phone
                })
                .ToListAsync();

            var existingStoneIds = await _context.WorkOrderDetails
                .AsNoTracking()
                .Where(x => x.WorkOrderId == workOrderId)
                .Select(x => x.StoneId)
                .Distinct()
                .ToListAsync();

            ViewBag.Stones = await _context.Stones
                .AsNoTracking()
                .Where(x =>
                    x.IsActive ||
                    existingStoneIds.Contains(x.StoneId))
                .OrderBy(x => x.StoneName)
                .Select(x => new
                {
                    x.StoneId,
                    x.StoneName,
                    x.SalePrice,

                    CurrentStock = x.StockMovements
                        .Select(m =>
                            m.MovementType == StockMovementType.Entry ||
                            m.MovementType == StockMovementType.AdjustmentIn ||
                            m.MovementType == StockMovementType.WorkOrderReturn
                                ? m.Quantity
                                : -m.Quantity)
                        .Sum()
                })
                .ToListAsync();
        }

        // Verilen taşların güncel plaka stoklarını hesaplar
        private async Task<Dictionary<int, int>> GetCurrentStocksAsync(
            List<int> stoneIds)
        {
            return await _context.StockMovements
                .AsNoTracking()
                .Where(x => stoneIds.Contains(x.StoneId))
                .GroupBy(x => x.StoneId)
                .Select(g => new
                {
                    StoneId = g.Key,

                    CurrentStock = g.Sum(x =>
                        x.MovementType == StockMovementType.Entry ||
                        x.MovementType == StockMovementType.AdjustmentIn ||
                        x.MovementType == StockMovementType.WorkOrderReturn
                            ? x.Quantity
                            : -x.Quantity)
                })
                .ToDictionaryAsync(
                    x => x.StoneId,
                    x => x.CurrentStock);
        }

        // İş emrinin KDV hariç, KDV ve KDV dahil toplamlarını hesaplar
        private static void CalculateTotals(
            IEnumerable<PriceLine> details,
            decimal cuttingMeter,
            decimal cuttingPricePerMeter,
            decimal vatRate,
            out decimal subTotal,
            out decimal vatAmount,
            out decimal totalAmount)
        {
            decimal stoneTotal = details
                .Sum(x => x.Quantity * x.UnitPrice);

            decimal cuttingTotal =
                cuttingMeter * cuttingPricePerMeter;

            subTotal = Math.Round(
                stoneTotal + cuttingTotal,
                2,
                MidpointRounding.AwayFromZero);

            vatAmount = Math.Round(
                subTotal * vatRate / 100m,
                2,
                MidpointRounding.AwayFromZero);

            totalAmount = subTotal + vatAmount;
        }

        // Verilen tarihin yılına göre sıradaki iş emri numarasını üretir
        // Örnek: IE-2026-000001
        private async Task<string> GenerateWorkOrderNumberAsync(DateTime createdDate)
        {
            int year = createdDate.Year;
            string prefix = $"IE-{year}-";

            string? lastWorkOrderNumber = await _context.WorkOrders
                .AsNoTracking()
                .Where(x => x.WorkOrderNumber.StartsWith(prefix))
                .OrderByDescending(x => x.WorkOrderNumber)
                .Select(x => x.WorkOrderNumber)
                .FirstOrDefaultAsync();

            int nextSequence = 1;

            if (!string.IsNullOrWhiteSpace(lastWorkOrderNumber))
            {
                string sequencePart = lastWorkOrderNumber.Substring(prefix.Length);

                if (int.TryParse(sequencePart, out int lastSequence))
                {
                    nextSequence = lastSequence + 1;
                }
            }

            return $"{prefix}{nextSequence:D6}";
        }

        // Boş veya yalnızca boşluk içeren metinleri null olarak kaydeder
        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        // Toplam hesaplamasında yalnızca gerekli fiyat bilgilerini taşır
        private sealed record PriceLine(
            int Quantity,
            decimal UnitPrice);
    }
}
