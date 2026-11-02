using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using StoneTech.Data;
using StoneTech.Entities;
using StoneTech.Enums;
using Microsoft.AspNetCore.Authorization;
using StoneTech.Models.Stock;
using StoneTech.Services;

namespace StoneTech.Controllers
{
    // Stok durumuna Admin, Manager, Sales ve Workshop rolleri erişebilir
    [Authorize(Roles = "Admin,Manager,Sales,Workshop")]
    public class StockController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogService _activityLogService;

        // Veritabanı ve işlem geçmişi servisleri alınır
        public StockController(
            AppDbContext context,
            IActivityLogService activityLogService)
        {
            _context = context;
            _activityLogService = activityLogService;
        }

        // Taşların güncel stok durumlarını listeler
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            int page = 1,
            int pageSize = 10)
        {
            // Sayfa numarasının 1'den küçük olmasını engeller
            if (page < 1)
            {
                page = 1;
            }

            // Kullanıcının seçebileceği sayfa boyutları
            var allowedPageSizes = new[] { 5, 10, 20, 50 };

            // Geçersiz bir sayfa boyutu gelirse varsayılan olarak 10 kullanılır
            if (!allowedPageSizes.Contains(pageSize))
            {
                pageSize = 10;
            }

            // Arama metninin başındaki ve sonundaki boşlukları temizler
            search = search?.Trim();

            // Taşlar üzerinden stok listesi sorgusu hazırlanır
            var query = _context.Stones
                .AsNoTracking()
                .AsQueryable();

            // Taş adına veya renge göre arama yapar
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.StoneName.Contains(search) ||
                    (x.Color != null && x.Color.Contains(search)));
            }

            // Filtreye uyan toplam taş sayısını bulur
            int totalCount = await query.CountAsync();

            // Toplam sayfa sayısını hesaplar
            int totalPages = (int)Math.Ceiling(
                totalCount / (double)pageSize);

            // Kullanıcı mevcut sayfa sayısından büyük bir sayfaya giderse
            // son sayfaya yönlendirilir
            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            // Taşların mevcut stok miktarlarını stok hareketlerinden hesaplar
            var stocks = await query
                .OrderBy(x => x.StoneName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new StockListViewModel
                {
                    StoneId = x.StoneId,
                    StoneName = x.StoneName,
                    Color = x.Color,
                    ImageUrl = x.ImageUrl,
                    IsActive = x.IsActive,

                    // Giriş ve düzeltme girişleri stoğu artırır.
                    // Çıkış ve düzeltme çıkışları stoğu azaltır.
                    CurrentStock = x.StockMovements
                        .Sum(m => (int?)(
                            m.MovementType == StockMovementType.Entry ||
                            m.MovementType == StockMovementType.AdjustmentIn ||
                            m.MovementType == StockMovementType.WorkOrderReturn
                                ? m.Quantity
                                : -m.Quantity)) ?? 0,

                    // Stone tablosundaki PurchasePrice alanı
                    // güncel ağırlıklı ortalama alış maliyetini tutar
                    AveragePurchasePrice = x.PurchasePrice,

                    // Stok değeri aşağıda hesaplanacağı için
                    // burada başlangıç değeri verilir
                    StockValue = 0
                })
                .ToListAsync();

            // Her taşın toplam stok değerini hesaplar
            foreach (var stock in stocks)
            {
                stock.StockValue =
                    stock.CurrentStock *
                    stock.AveragePurchasePrice;
            }

            // Sayfalama ve arama bilgilerini View'a gönderir
            ViewBag.Search = search ?? "";
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View(stocks);
        }

        // Stok hareketlerini listeler
        // Taş Id gönderilirse yalnızca o taşa ait hareketleri gösterir
        [HttpGet]
        public async Task<IActionResult> Movements(
            int? id,
            int page = 1,
            int pageSize = 20)
        {
            // Sayfa numarasının 1'den küçük olmasını engeller
            if (page < 1)
            {
                page = 1;
            }

            // Hareket geçmişinde kullanılabilecek sayfa boyutları
            var allowedPageSizes = new[] { 10, 20, 50, 100 };

            // Geçersiz sayfa boyutu gelirse varsayılan olarak 20 kullanılır
            if (!allowedPageSizes.Contains(pageSize))
            {
                pageSize = 20;
            }

            // Bütün stok hareketleri için temel sorguyu hazırlar
            var query = _context.StockMovements
                .AsNoTracking()
                .AsQueryable();

            // Taş Id gönderildiyse yalnızca o taşın hareketlerini gösterir
            if (id.HasValue)
            {
                var stone = await _context.Stones
                    .AsNoTracking()
                    .Where(x => x.StoneId == id.Value)
                    .Select(x => new
                    {
                        x.StoneId,
                        x.StoneName,
                        x.Color
                    })
                    .FirstOrDefaultAsync();

                if (stone == null)
                {
                    return NotFound();
                }

                ViewBag.StoneId = stone.StoneId;
                ViewBag.StoneName = stone.StoneName;
                ViewBag.Color = stone.Color;
                ViewBag.CurrentStock =
                    await GetCurrentStockAsync(stone.StoneId);

                query = query.Where(x =>
                    x.StoneId == id.Value);
            }
            else
            {
                ViewBag.StoneId = 0;
                ViewBag.StoneName = "";
                ViewBag.Color = null;
                ViewBag.CurrentStock = 0;
            }

            // Toplam hareket sayısını bulur
            int totalCount = await query.CountAsync();

            // Toplam sayfa sayısını hesaplar
            int totalPages = (int)Math.Ceiling(
                totalCount / (double)pageSize);

            // Geçersiz yüksek sayfa numarasını son sayfaya çeker
            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            // Stok hareketlerini en yeni hareket üstte olacak şekilde getirir
            var movements = await query
                .OrderByDescending(x => x.MovementDate)
                .ThenByDescending(x => x.StockMovementId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new StockMovementListViewModel
                {
                    StockMovementId = x.StockMovementId,
                    StoneId = x.StoneId,
                    StoneName = x.Stone.StoneName,
                    Quantity = x.Quantity,
                    MovementType = x.MovementType,
                    MovementDate = x.MovementDate,
                    Description = x.Description,
                    PurchaseId = x.PurchaseId,
                    PurchaseNumber = x.Purchase != null
                        ? x.Purchase.PurchaseNumber
                        : null,
                    WorkOrderId = x.WorkOrderId
                })
                .ToListAsync();

            // Sayfalama bilgilerini View'a gönderir
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View(movements);
        }

        // Manuel stok düzeltme ekranını açar
        // Stok miktarını doğrudan değiştirebildiği için yalnızca Admin ve Manager kullanabilir
        [Authorize(Roles = "Admin,Manager")]
        [HttpGet]
        public async Task<IActionResult> Adjustment(int id)
        {
            // Düzeltme yapılacak taşı getirir
            var stone = await _context.Stones
                .AsNoTracking()
                .Where(x => x.StoneId == id)
                .Select(x => new
                {
                    x.StoneId,
                    x.StoneName,
                    x.IsActive
                })
                .FirstOrDefaultAsync();

            if (stone == null)
            {
                return NotFound();
            }

            // Pasif taşlarda manuel stok düzeltmesi yapılmasını engeller
            if (!stone.IsActive)
            {
                TempData["ErrorMessage"] =
                    "Pasif bir taş için stok düzeltmesi yapılamaz.";

                return RedirectToAction(nameof(Index));
            }

            // Taşın mevcut stok miktarını hesaplar
            int currentStock =
                await GetCurrentStockAsync(stone.StoneId);

            // Düzeltme ekranına gönderilecek model hazırlanır
            var model = new StockAdjustmentViewModel
            {
                StoneId = stone.StoneId,
                StoneName = stone.StoneName,
                CurrentStock = currentStock,

                // Varsayılan olarak stok artırma seçilir
                MovementType =
                    StockMovementType.AdjustmentIn,

                Quantity = 1
            };

            return View(model);
        }

        // Manuel stok düzeltmesini kaydeder
        // Stok miktarını doğrudan değiştirebildiği için yalnızca Admin ve Manager kullanabilir
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adjustment(
            StockAdjustmentViewModel model)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            // Taşın veritabanında gerçekten var ve aktif
            // olduğunu tekrar kontrol eder
            var stone = await _context.Stones
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.StoneId == model.StoneId &&
                    x.IsActive);

            if (stone == null)
            {
                return NotFound();
            }

            // Kullanıcının formu değiştirerek normal Entry veya Exit
            // göndermesini engeller. Manuel işlemde yalnızca
            // AdjustmentIn ve AdjustmentOut kullanılabilir.
            if (model.MovementType !=
                    StockMovementType.AdjustmentIn &&
                model.MovementType !=
                    StockMovementType.AdjustmentOut)
            {
                ModelState.AddModelError(
                    nameof(model.MovementType),
                    "Geçersiz stok düzeltme tipi.");
            }

            // Mevcut stok miktarını sunucu tarafında yeniden hesaplar
            int currentStock =
                await GetCurrentStockAsync(model.StoneId);

            // Formdan gelen StoneName ve CurrentStock değerlerine güvenilmez.
            // Gerçek değerler veritabanından tekrar doldurulur.
            model.StoneName = stone.StoneName;
            model.CurrentStock = currentStock;

            // Stok azaltılıyorsa mevcut stoktan daha fazla
            // plaka çıkarılmasını engeller
            if (model.MovementType ==
                    StockMovementType.AdjustmentOut &&
                model.Quantity > currentStock)
            {
                ModelState.AddModelError(
                    nameof(model.Quantity),
                    $"Mevcut stok {currentStock} plakadır. " +
                    "Stok miktarından daha fazla çıkış yapılamaz.");
            }

            if (!ModelState.IsValid)
            {
                await transaction.RollbackAsync();
                return View(model);
            }

            // Açıklamadaki gereksiz baş ve son boşlukları temizler
            string description = model.Description.Trim();

            // Manuel stok düzeltme hareketini oluşturur
            var stockMovement = new StockMovement
            {
                StoneId = model.StoneId,
                Quantity = model.Quantity,
                MovementType = model.MovementType,
                MovementDate = DateTime.Now,
                Description = description,

                // Manuel düzeltme bir alıma veya iş emrine bağlı değildir
                PurchaseId = null,
                WorkOrderId = null
            };

            try
            {
                _context.StockMovements.Add(stockMovement);

                await _context.SaveChangesAsync();

                // Manuel stok düzeltmesini işlem geçmişine kaydeder
                if (model.MovementType ==
                    StockMovementType.AdjustmentIn)
                {
                    await _activityLogService.LogAsync(
                        "Stock",
                        "Adjustment",
                        stockMovement.StockMovementId,
                        $"{stone.StoneName} stoğuna manuel olarak " +
                        $"{model.Quantity} plaka eklendi. " +
                        $"Açıklama: {description}");
                }
                else
                {
                    await _activityLogService.LogAsync(
                        "Stock",
                        "Adjustment",
                        stockMovement.StockMovementId,
                        $"{stone.StoneName} stoğundan manuel olarak " +
                        $"{model.Quantity} plaka düşüldü. " +
                        $"Açıklama: {description}");
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "Stok düzeltmesi kaydedilemedi. Stok değişmiş olabilir; lütfen tekrar deneyin.");

                return View(model);
            }

            TempData["SuccessMessage"] =
                model.MovementType ==
                StockMovementType.AdjustmentIn
                    ? $"{model.Quantity} plaka stoğa eklendi."
                    : $"{model.Quantity} plaka stoktan düşüldü.";

            return RedirectToAction(
                nameof(Movements),
                new { id = model.StoneId });
        }

        // Belirtilen taşın güncel stok miktarını hesaplar
        private async Task<int> GetCurrentStockAsync(
            int stoneId)
        {
            // Giriş ve düzeltme girişleri pozitif,
            // çıkış ve düzeltme çıkışları negatif hesaplanır
            int currentStock =
                await _context.StockMovements
                    .Where(x => x.StoneId == stoneId)
                    .SumAsync(x => (int?)(
                        x.MovementType ==
                            StockMovementType.Entry ||
                        x.MovementType ==
                            StockMovementType.AdjustmentIn ||
                        x.MovementType ==
                            StockMovementType.WorkOrderReturn
                            ? x.Quantity
                            : -x.Quantity)) ?? 0;

            return currentStock;
        }
    }
}
