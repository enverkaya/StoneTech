using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.EntityFrameworkCore;
using StoneTech.Data;
using StoneTech.Entities;
using StoneTech.Enums;
using StoneTech.Models.Purchase;
using StoneTech.Services;

namespace StoneTech.Controllers
{
    // Alım modülüne Admin, Manager, Sales ve Accountant rolleri erişebilir
    [Authorize(Roles = "Admin,Manager,Sales,Accountant")]
    public class PurchaseController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogService _activityLogService;

        // Veritabanı ve işlem geçmişi servisleri alınır
        public PurchaseController(
            AppDbContext context,
            IActivityLogService activityLogService)
        {
            _context = context;
            _activityLogService = activityLogService;
        }

        // Alımları listeler
        [HttpGet]
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

            var query = _context.Purchases
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.PurchaseNumber.Contains(search) ||
                    x.Supplier.SupplierName.Contains(search) ||
                    (x.Description != null &&
                     x.Description.Contains(search)));
            }

            var totalCount = await query.CountAsync();

            var totalPages = (int)Math.Ceiling(
                totalCount / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var purchases = await query
                .OrderByDescending(x => x.PurchaseDate)
                .ThenByDescending(x => x.PurchaseId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new PurchaseListViewModel
                {
                    PurchaseId = x.PurchaseId,
                    PurchaseNumber = x.PurchaseNumber,
                    SupplierName = x.Supplier.SupplierName,
                    PurchaseDate = x.PurchaseDate,
                    TotalAmount = x.TotalAmount,

                    DetailCount = x.PurchaseDetails.Count(),

                    TotalQuantity = x.PurchaseDetails
                        .Sum(d => (int?)d.Quantity) ?? 0,

                    Description = x.Description
                })
                .ToListAsync();

            ViewBag.Search = search ?? "";
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View(purchases);
        }

        // Yeni alım ekranını açar
        [Authorize(Roles = "Admin,Manager,Sales")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new PurchaseCreateViewModel
            {
                PurchaseDate = DateTime.Today,

                Details = new List<PurchaseDetailCreateViewModel>
                {
                    new PurchaseDetailCreateViewModel()
                }
            };

            await LoadCreateSelectionsAsync();

            return View(model);
        }

        // Yeni alımı kaydeder
        [Authorize(Roles = "Admin,Manager,Sales")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            PurchaseCreateViewModel model)
        {
            model.Details ??=
                new List<PurchaseDetailCreateViewModel>();

            if (model.Details.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(model.Details),
                    "Alıma en az bir taş eklemelisiniz.");
            }

            if (!ModelState.IsValid)
            {
                await LoadCreateSelectionsAsync();
                return View(model);
            }

            var supplierExists = await _context.Suppliers
                .AnyAsync(x =>
                    x.SupplierId == model.SupplierId &&
                    x.IsActive);

            if (!supplierExists)
            {
                ModelState.AddModelError(
                    nameof(model.SupplierId),
                    "Seçilen tedarikçi bulunamadı veya pasif durumda.");

                await LoadCreateSelectionsAsync();
                return View(model);
            }

            var duplicateStone = model.Details
                .GroupBy(x => x.StoneId)
                .Any(x => x.Count() > 1);

            if (duplicateStone)
            {
                ModelState.AddModelError(
                    nameof(model.Details),
                    "Aynı taşı bir alıma birden fazla kez ekleyemezsiniz.");

                await LoadCreateSelectionsAsync();
                return View(model);
            }

            var stoneIds = model.Details
                .Select(x => x.StoneId)
                .Distinct()
                .ToList();

            var validStoneIds = await _context.Stones
                .Where(x =>
                    stoneIds.Contains(x.StoneId) &&
                    x.IsActive)
                .Select(x => x.StoneId)
                .ToListAsync();

            if (validStoneIds.Count != stoneIds.Count)
            {
                ModelState.AddModelError(
                    nameof(model.Details),
                    "Seçilen taşlardan biri bulunamadı veya pasif durumda.");

                await LoadCreateSelectionsAsync();
                return View(model);
            }

            decimal totalAmount = model.Details
                .Sum(x => x.Quantity * x.UnitPrice);

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                string purchaseNumber =
                    await GeneratePurchaseNumberAsync(
                        model.PurchaseDate.Year);

                var purchase = new Purchase
                {
                    PurchaseNumber = purchaseNumber,
                    SupplierId = model.SupplierId,
                    PurchaseDate = model.PurchaseDate,
                    TotalAmount = totalAmount,

                    Description =
                        string.IsNullOrWhiteSpace(model.Description)
                            ? null
                            : model.Description.Trim()
                };

                _context.Purchases.Add(purchase);

                await _context.SaveChangesAsync();

                foreach (var detailModel in model.Details)
                {
                    var stone = await _context.Stones
                        .FirstAsync(x =>
                            x.StoneId == detailModel.StoneId);

                    int currentStock = await GetCurrentStockAsync(
                        detailModel.StoneId);

                    decimal currentStockValue =
                        currentStock * stone.PurchasePrice;

                    decimal newPurchaseValue =
                        detailModel.Quantity *
                        detailModel.UnitPrice;

                    int newStockQuantity =
                        currentStock +
                        detailModel.Quantity;

                    decimal newAveragePurchasePrice =
                        newStockQuantity > 0
                            ? (currentStockValue + newPurchaseValue)
                              / newStockQuantity
                            : detailModel.UnitPrice;

                    stone.PurchasePrice =
                        Math.Round(
                            newAveragePurchasePrice,
                            2,
                            MidpointRounding.AwayFromZero);

                    var purchaseDetail = new PurchaseDetail
                    {
                        PurchaseId = purchase.PurchaseId,
                        StoneId = detailModel.StoneId,
                        Quantity = detailModel.Quantity,
                        UnitPrice = detailModel.UnitPrice
                    };

                    _context.PurchaseDetails.Add(purchaseDetail);

                    var stockMovement = new StockMovement
                    {
                        StoneId = detailModel.StoneId,
                        Quantity = detailModel.Quantity,
                        MovementType = StockMovementType.Entry,
                        MovementDate = model.PurchaseDate,
                        PurchaseId = purchase.PurchaseId,
                        WorkOrderId = null,

                        Description =
                            $"Alım {purchase.PurchaseNumber} stok girişi"
                    };

                    _context.StockMovements.Add(stockMovement);
                }

                await _context.SaveChangesAsync();

                // Alım oluşturma işlemini işlem geçmişine kaydeder
                await _activityLogService.LogAsync(
                    "Purchase",
                    "Create",
                    purchase.PurchaseId,
                    $"{purchase.PurchaseNumber} numaralı alım oluşturuldu.");

                await transaction.CommitAsync();

                return RedirectToAction(
                    nameof(Details),
                    new { id = purchase.PurchaseId });
            }
            catch
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "Alım kaydedilirken bir hata oluştu. İşlem geri alındı.");

                await LoadCreateSelectionsAsync();

                return View(model);
            }
        }

        // Alım düzenleme ekranını yalnızca Admin ve Manager açabilir
        [Authorize(Roles = "Admin,Manager")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var purchase = await _context.Purchases
                .AsNoTracking()
                .Where(x => x.PurchaseId == id)
                .Select(x => new PurchaseEditViewModel
                {
                    PurchaseId = x.PurchaseId,
                    PurchaseNumber = x.PurchaseNumber,
                    SupplierId = x.SupplierId,
                    PurchaseDate = x.PurchaseDate,
                    Description = x.Description,

                    Details = x.PurchaseDetails
                        .OrderBy(x => x.PurchaseDetailId)
                        .Select(x => new PurchaseDetailEditViewModel
                        {
                            PurchaseDetailId =
                                x.PurchaseDetailId,

                            StoneId = x.StoneId,
                            Quantity = x.Quantity,
                            UnitPrice = x.UnitPrice
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (purchase == null)
            {
                return NotFound();
            }

            await LoadEditSelectionsAsync(
                purchase.SupplierId,
                purchase.Details
                    .Select(x => x.StoneId)
                    .ToList());

            return View(purchase);
        }

        // Alım kaydındaki hataları yalnızca Admin ve Manager düzeltebilir
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            PurchaseEditViewModel model)
        {
            if (id != model.PurchaseId)
            {
                return NotFound();
            }

            model.Details ??=
                new List<PurchaseDetailEditViewModel>();

            if (model.Details.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(model.Details),
                    "Alımda en az bir taş bulunmalıdır.");
            }

            // Stok uygunluk kontrolü ile eski/yeni stok hareketlerinin değişimini
            // aynı seri hale getirilebilir transaction içinde tutar.
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            var purchase = await _context.Purchases
                .Include(x => x.PurchaseDetails)
                .Include(x => x.StockMovements)
                .FirstOrDefaultAsync(x =>
                    x.PurchaseId == id);

            if (purchase == null)
            {
                return NotFound();
            }

            // Kullanıcı form üzerinden alım numarasını değiştiremez.
            model.PurchaseNumber = purchase.PurchaseNumber;

            var supplierExists = await _context.Suppliers
                .AnyAsync(x =>
                    x.SupplierId == model.SupplierId &&
                    (x.IsActive ||
                     x.SupplierId == purchase.SupplierId));

            if (!supplierExists)
            {
                ModelState.AddModelError(
                    nameof(model.SupplierId),
                    "Seçilen tedarikçi bulunamadı veya pasif durumda.");
            }

            var duplicateStone = model.Details
                .GroupBy(x => x.StoneId)
                .Any(x => x.Count() > 1);

            if (duplicateStone)
            {
                ModelState.AddModelError(
                    nameof(model.Details),
                    "Aynı taşı bir alıma birden fazla kez ekleyemezsiniz.");
            }

            var newStoneIds = model.Details
                .Select(x => x.StoneId)
                .Distinct()
                .ToList();

            if (newStoneIds.Count > 0)
            {
                var validStoneIds = await _context.Stones
                    .Where(x =>
                        newStoneIds.Contains(x.StoneId) &&
                        x.IsActive)
                    .Select(x => x.StoneId)
                    .ToListAsync();

                // Edit edilen alımda önceden bulunan pasif taşların
                // düzenlenebilmesine izin verir.
                var oldStoneIds = purchase.PurchaseDetails
                    .Select(x => x.StoneId)
                    .Distinct()
                    .ToList();

                var invalidStoneIds = newStoneIds
                    .Where(x =>
                        !validStoneIds.Contains(x) &&
                        !oldStoneIds.Contains(x))
                    .ToList();

                if (invalidStoneIds.Count > 0)
                {
                    ModelState.AddModelError(
                        nameof(model.Details),
                        "Seçilen taşlardan biri bulunamadı veya pasif durumda.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadEditSelectionsAsync(
                    purchase.SupplierId,
                    purchase.PurchaseDetails
                        .Select(x => x.StoneId)
                        .Concat(newStoneIds)
                        .Distinct()
                        .ToList());

                return View(model);
            }

            // Eski ve yeni alımda bulunan bütün taşları toplar.
            // Böylece alımdan tamamen çıkarılan taşlar da kontrol edilir.
            var affectedStoneIds = purchase.PurchaseDetails
                .Select(x => x.StoneId)
                .Concat(newStoneIds)
                .Distinct()
                .ToList();

            // Edit sonrasında herhangi bir taşın mevcut stoğunun
            // eksiye düşüp düşmeyeceğini kontrol eder.
            foreach (int stoneId in affectedStoneIds)
            {
                int currentStock =
                    await GetCurrentStockAsync(stoneId);

                int oldPurchaseQuantity =
                    purchase.PurchaseDetails
                        .Where(x => x.StoneId == stoneId)
                        .Sum(x => x.Quantity);

                int newPurchaseQuantity =
                    model.Details
                        .Where(x => x.StoneId == stoneId)
                        .Sum(x => x.Quantity);

                int stockAfterEdit =
                    currentStock -
                    oldPurchaseQuantity +
                    newPurchaseQuantity;

                if (stockAfterEdit < 0)
                {
                    var stoneName = await _context.Stones
                        .Where(x => x.StoneId == stoneId)
                        .Select(x => x.StoneName)
                        .FirstOrDefaultAsync();

                    ModelState.AddModelError(
                        nameof(model.Details),
                        $"{stoneName} için alım miktarı bu kadar azaltılamaz. " +
                        "Düzenleme sonucunda stok eksiye düşer.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadEditSelectionsAsync(
                    purchase.SupplierId,
                    affectedStoneIds);

                return View(model);
            }

            try
            {
                // Ana alım bilgilerini günceller.
                // PurchaseNumber özellikle değiştirilmez.
                purchase.SupplierId = model.SupplierId;
                purchase.PurchaseDate = model.PurchaseDate;

                purchase.Description =
                    string.IsNullOrWhiteSpace(model.Description)
                        ? null
                        : model.Description.Trim();

                purchase.TotalAmount =
                    model.Details.Sum(x =>
                        x.Quantity * x.UnitPrice);

                // Bu alıma ait eski stok girişlerini kaldırır.
                // Diğer alımların veya iş emirlerinin stok hareketlerine dokunmaz.
                var oldStockMovements =
                    purchase.StockMovements
                        .Where(x =>
                            x.PurchaseId == purchase.PurchaseId)
                        .ToList();

                _context.StockMovements.RemoveRange(
                    oldStockMovements);

                // Eski alım detaylarını kaldırır.
                // Düzeltilmiş bilgiler aşağıda yeniden oluşturulur.
                _context.PurchaseDetails.RemoveRange(
                    purchase.PurchaseDetails);

                // Önce eski kayıtları veritabanından kaldırır.
                // Transaction henüz commit edilmediği için hata olursa
                // bütün işlem geri alınabilir.
                await _context.SaveChangesAsync();

                // Düzeltilmiş alım detaylarını ve stok girişlerini oluşturur.
                foreach (var detailModel in model.Details)
                {
                    var purchaseDetail =
                        new PurchaseDetail
                        {
                            PurchaseId =
                                purchase.PurchaseId,

                            StoneId =
                                detailModel.StoneId,

                            Quantity =
                                detailModel.Quantity,

                            UnitPrice =
                                detailModel.UnitPrice
                        };

                    _context.PurchaseDetails.Add(
                        purchaseDetail);

                    var stockMovement =
                        new StockMovement
                        {
                            StoneId =
                                detailModel.StoneId,

                            Quantity =
                                detailModel.Quantity,

                            MovementType =
                                StockMovementType.Entry,

                            MovementDate =
                                model.PurchaseDate,

                            PurchaseId =
                                purchase.PurchaseId,

                            WorkOrderId = null,

                            Description =
                                $"Alım {purchase.PurchaseNumber} stok girişi"
                        };

                    _context.StockMovements.Add(
                        stockMovement);
                }

                await _context.SaveChangesAsync();

                // Eski alım değiştiği için etkilenen bütün taşların
                // mevcut ortalama alış maliyetlerini yeniden hesaplar.
                foreach (int stoneId in affectedStoneIds)
                {
                    await RecalculateStonePurchasePriceAsync(
                        stoneId);
                }

                await _context.SaveChangesAsync();

                // Alım güncelleme işlemini işlem geçmişine kaydeder
                await _activityLogService.LogAsync(
                    "Purchase",
                    "Update",
                    purchase.PurchaseId,
                    $"{purchase.PurchaseNumber} numaralı alım güncellendi.");

                await transaction.CommitAsync();

                return RedirectToAction(
                    nameof(Details),
                    new { id = purchase.PurchaseId });
            }
            catch
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "Alım güncellenirken bir hata oluştu. İşlem geri alındı.");

                await LoadEditSelectionsAsync(
                    purchase.SupplierId,
                    affectedStoneIds);

                return View(model);
            }
        }

        // Alım detaylarını görüntüler
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var purchase = await _context.Purchases
                .AsNoTracking()
                .Where(x => x.PurchaseId == id)
                .Select(x => new PurchaseDetailsViewModel
                {
                    PurchaseId = x.PurchaseId,
                    PurchaseNumber = x.PurchaseNumber,

                    SupplierName = x.Supplier.SupplierName,
                    SupplierPhone = x.Supplier.Phone,

                    PurchaseDate = x.PurchaseDate,
                    TotalAmount = x.TotalAmount,
                    Description = x.Description,

                    Details = x.PurchaseDetails
                        .OrderBy(d => d.Stone.StoneName)
                        .Select(d =>
                            new PurchaseDetailsItemViewModel
                            {
                                StoneId = d.StoneId,
                                StoneName = d.Stone.StoneName,
                                Quantity = d.Quantity,
                                UnitPrice = d.UnitPrice,

                                LineTotal =
                                    d.Quantity * d.UnitPrice
                            })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (purchase == null)
            {
                return NotFound();
            }

            return View(purchase);
        }

        // Taşın mevcut stok miktarını hesaplar
        private async Task<int> GetCurrentStockAsync(
            int stoneId)
        {
            return await _context.StockMovements
                .Where(x => x.StoneId == stoneId)
                .SumAsync(x =>
                    x.MovementType == StockMovementType.Entry ||
                    x.MovementType == StockMovementType.AdjustmentIn ||
                    x.MovementType == StockMovementType.WorkOrderReturn
                        ? x.Quantity
                        : -x.Quantity);
        }

        // Alım düzenlendiğinde taşın ortalama alış maliyetini yeniden hesaplar
        private async Task RecalculateStonePurchasePriceAsync(
            int stoneId)
        {
            var stone = await _context.Stones
                .FirstAsync(x => x.StoneId == stoneId);

            var movements = await _context.StockMovements
                .AsNoTracking()
                .Where(x => x.StoneId == stoneId)
                .OrderBy(x => x.MovementDate)
                .ThenBy(x => x.StockMovementId)
                .ToListAsync();

            var purchaseIds = movements
                .Where(x =>
                    x.PurchaseId.HasValue &&
                    x.MovementType == StockMovementType.Entry)
                .Select(x => x.PurchaseId!.Value)
                .Distinct()
                .ToList();

            var purchasePrices =
                await _context.PurchaseDetails
                    .AsNoTracking()
                    .Where(x =>
                        x.StoneId == stoneId &&
                        purchaseIds.Contains(x.PurchaseId))
                    .ToDictionaryAsync(
                        x => x.PurchaseId,
                        x => x.UnitPrice);

            int stockQuantity = 0;
            decimal stockValue = 0;

            foreach (var movement in movements)
            {
                if (movement.MovementType == StockMovementType.Entry)
                {
                    decimal unitPrice = 0;

                    if (movement.PurchaseId.HasValue &&
                        purchasePrices.TryGetValue(
                            movement.PurchaseId.Value,
                            out decimal purchasePrice))
                    {
                        unitPrice = purchasePrice;
                    }
                    else if (stockQuantity > 0)
                    {
                        unitPrice = stockValue / stockQuantity;
                    }

                    stockValue += movement.Quantity * unitPrice;
                    stockQuantity += movement.Quantity;
                }
                else if (
                    movement.MovementType == StockMovementType.AdjustmentIn ||
                    movement.MovementType == StockMovementType.WorkOrderReturn)
                {
                    // Stok girişinde mevcut ortalama maliyet korunur
                    decimal currentAverage =
                        stockQuantity > 0
                            ? stockValue / stockQuantity
                            : stone.PurchasePrice;

                    stockValue +=
                        movement.Quantity * currentAverage;

                    stockQuantity +=
                        movement.Quantity;
                }
                else if (
                    movement.MovementType == StockMovementType.Exit ||
                    movement.MovementType == StockMovementType.AdjustmentOut)
                {
                    if (stockQuantity <= 0)
                    {
                        continue;
                    }

                    decimal currentAverage =
                        stockValue / stockQuantity;

                    int quantityToRemove =
                        Math.Min(
                            movement.Quantity,
                            stockQuantity);

                    stockValue -=
                        quantityToRemove * currentAverage;

                    stockQuantity -=
                        quantityToRemove;

                    if (stockQuantity == 0)
                    {
                        stockValue = 0;
                    }
                }
            }

            // Stok varsa güncel ortalama alış maliyetini kaydeder
            if (stockQuantity > 0)
            {
                stone.PurchasePrice =
                    Math.Round(
                        stockValue / stockQuantity,
                        2,
                        MidpointRounding.AwayFromZero);
            }
        }

        // Belirtilen yıl için sıradaki alım numarasını oluşturur
        private async Task<string> GeneratePurchaseNumberAsync(
            int year)
        {
            string prefix = $"ALM-{year}-";

            string? lastPurchaseNumber =
                await _context.Purchases
                    .AsNoTracking()
                    .Where(x =>
                        x.PurchaseNumber.StartsWith(prefix))
                    .OrderByDescending(x =>
                        x.PurchaseNumber)
                    .Select(x =>
                        x.PurchaseNumber)
                    .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (!string.IsNullOrWhiteSpace(
                lastPurchaseNumber))
            {
                string numberPart =
                    lastPurchaseNumber.Substring(
                        prefix.Length);

                if (int.TryParse(
                    numberPart,
                    out int lastNumber))
                {
                    nextNumber =
                        lastNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D6}";
        }

        // Create ekranındaki aktif tedarikçi ve taşları yükler
        private async Task LoadCreateSelectionsAsync()
        {
            ViewBag.Suppliers =
                await _context.Suppliers
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.SupplierName)
                    .Select(x => new
                    {
                        x.SupplierId,
                        x.SupplierName
                    })
                    .ToListAsync();

            ViewBag.Stones =
                await _context.Stones
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.StoneName)
                    .Select(x => new
                    {
                        x.StoneId,
                        x.StoneName,
                        x.PurchasePrice
                    })
                    .ToListAsync();
        }

        // Edit ekranındaki tedarikçi ve taş listelerini yükler.
        // Mevcut kayıtta kullanılan pasif kayıtlar da
        // düzenleme ekranında kaybolmaz.
        private async Task LoadEditSelectionsAsync(
            int currentSupplierId,
            List<int> currentStoneIds)
        {
            ViewBag.Suppliers =
                await _context.Suppliers
                    .AsNoTracking()
                    .Where(x =>
                        x.IsActive ||
                        x.SupplierId == currentSupplierId)
                    .OrderBy(x => x.SupplierName)
                    .Select(x => new
                    {
                        x.SupplierId,
                        x.SupplierName
                    })
                    .ToListAsync();

            ViewBag.Stones =
                await _context.Stones
                    .AsNoTracking()
                    .Where(x =>
                        x.IsActive ||
                        currentStoneIds.Contains(
                            x.StoneId))
                    .OrderBy(x => x.StoneName)
                    .Select(x => new
                    {
                        x.StoneId,
                        x.StoneName,
                        x.PurchasePrice
                    })
                    .ToListAsync();
        }
    }
}
