
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoneTech.Data;
using StoneTech.Entities;
using StoneTech.Enums;
using StoneTech.Models.Stone;
using StoneTech.Services;

namespace StoneTech.Controllers
{
    // Taş modülüne Admin, Manager, Sales ve Workshop rolleri erişebilir
    [Authorize(Roles = "Admin,Manager,Sales,Workshop")]
    public class StoneController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IActivityLogService _activityLogService;

        // Veritabanı, wwwroot ve işlem geçmişi servisleri alınır
        public StoneController(
            AppDbContext context,
            IWebHostEnvironment webHostEnvironment,
            IActivityLogService activityLogService)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _activityLogService = activityLogService;
        }

        // Taşları listeler
        // Arama, sayfalama ve stok hesaplama işlemlerini yapar
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            int page = 1,
            int pageSize = 10)
        {
            // Geçersiz sayfa numarasını engeller
            if (page < 1)
            {
                page = 1;
            }

            // Kullanılabilecek sayfa boyutları
            var allowedPageSizes = new[] { 5, 10, 20, 50 };

            if (!allowedPageSizes.Contains(pageSize))
            {
                pageSize = 10;
            }

            // Arama metninin başındaki ve sonundaki boşlukları temizler
            search = search?.Trim();

            // Taş sorgusunu oluşturur
            var query = _context.Stones
                .AsNoTracking()
                .AsQueryable();

            // Arama yapılmışsa taş adı veya renk içinde arar
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.StoneName.Contains(search) ||
                    (x.Color != null && x.Color.Contains(search)));
            }

            // Filtre sonucundaki toplam kayıt sayısı
            var totalCount = await query.CountAsync();

            // Toplam sayfa sayısını hesaplar
            var totalPages = (int)Math.Ceiling(
                totalCount / (double)pageSize);

            // Kullanıcı olmayan bir sayfaya giderse son sayfaya çeker
            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            // Taşları ViewModel'e dönüştürür
            var stones = await query
                .OrderBy(x => x.StoneName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new StoneListViewModel
                {
                    StoneId = x.StoneId,
                    StoneName = x.StoneName,
                    Color = x.Color,
                    PurchasePrice = x.PurchasePrice,
                    SalePrice = x.SalePrice,
                    ImageUrl = x.ImageUrl,
                    IsActive = x.IsActive,

                    // Girişleri ekler, çıkışları stoktan düşer
                    StockQuantity = x.StockMovements
                        .Select(s => (int?)(
                            s.MovementType == StockMovementType.Entry ||
                            s.MovementType == StockMovementType.AdjustmentIn ||
                            s.MovementType == StockMovementType.WorkOrderReturn
                                ? s.Quantity
                                : -s.Quantity))
                        .Sum() ?? 0
                })
                .ToListAsync();

            // Index View'inin arama ve pagination bilgileri
            ViewBag.Search = search ?? "";
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View(stones);
        }

        // Yeni taş ekleme sayfasını açar
        [Authorize(Roles = "Admin,Manager,Sales")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // Yeni taşı veritabanına kaydeder
        [Authorize(Roles = "Admin,Manager,Sales")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StoneCreateViewModel model)
        {
            // ViewModel validation hatası varsa sayfaya geri döner
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Taş adındaki gereksiz boşlukları temizler
            var stoneName = model.StoneName.Trim();

            // Aynı isimde başka taş var mı kontrol eder
            var stoneExists = await _context.Stones
                .AnyAsync(x => x.StoneName == stoneName);

            if (stoneExists)
            {
                ModelState.AddModelError(
                    nameof(model.StoneName),
                    "Bu isimde bir taş zaten kayıtlı.");

                return View(model);
            }

            string? imageUrl = null;

            // Görsel seçilmişse kontrol eder ve kaydeder
            if (model.ImageFile != null &&
                model.ImageFile.Length > 0)
            {
                var imageValidationError =
                    ValidateImage(model.ImageFile);

                if (imageValidationError != null)
                {
                    ModelState.AddModelError(
                        nameof(model.ImageFile),
                        imageValidationError);

                    return View(model);
                }

                imageUrl =
                    await SaveImageAsync(model.ImageFile);
            }

            // ViewModel'den yeni Stone Entity oluşturulur
            var stone = new Stone
            {
                StoneName = stoneName,

                Color = string.IsNullOrWhiteSpace(model.Color)
                    ? null
                    : model.Color.Trim(),

                PurchasePrice = model.PurchasePrice,
                SalePrice = model.SalePrice,
                ImageUrl = imageUrl,
                IsActive = true,

                // Web sitesinde gösterilecek taş açıklaması
                WebDescription = string.IsNullOrWhiteSpace(model.WebDescription)
                    ? null
                    : model.WebDescription.Trim(),

                // Taşın web sitesinde gösterilme durumu
                ShowOnWeb = model.ShowOnWeb
            };

            _context.Stones.Add(stone);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                DeleteImage(imageUrl);

                ModelState.AddModelError(
                    string.Empty,
                    "Taş kaydedilirken bir hata oluştu.");

                return View(model);
            }

            // Taş oluşturma işlemini işlem geçmişine kaydeder
            await _activityLogService.LogAsync(
                "Stone",
                "Create",
                stone.StoneId,
                $"{stone.StoneName} adlı taş oluşturuldu.");

            return RedirectToAction(nameof(Index));
        }

        // Taş düzenleme sayfasını açar
        [Authorize(Roles = "Admin,Manager")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var stone = await _context.Stones
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.StoneId == id);

            if (stone == null)
            {
                return NotFound();
            }

            // Entity bilgilerini Edit ViewModel'e aktarır
            var model = new StoneEditViewModel
            {
                StoneId = stone.StoneId,
                StoneName = stone.StoneName,
                Color = stone.Color,
                PurchasePrice = stone.PurchasePrice,
                SalePrice = stone.SalePrice,
                IsActive = stone.IsActive,
                CurrentImageUrl = stone.ImageUrl,

                // Web sitesi bilgilerini aktarır
                WebDescription = stone.WebDescription,
                ShowOnWeb = stone.ShowOnWeb
            };

            return View(model);
        }

        // Düzenlenen taşı veritabanında günceller
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            StoneEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Güncellenecek taşı bulur
            var stone = await _context.Stones
                .FirstOrDefaultAsync(
                    x => x.StoneId == model.StoneId);

            if (stone == null)
            {
                return NotFound();
            }

            var stoneName = model.StoneName.Trim();

            // Başka bir taş aynı ismi kullanıyor mu kontrol eder
            var stoneExists = await _context.Stones
                .AnyAsync(x =>
                    x.StoneId != model.StoneId &&
                    x.StoneName == stoneName);

            if (stoneExists)
            {
                // Validation hatasında mevcut görsel kaybolmasın
                model.CurrentImageUrl = stone.ImageUrl;

                ModelState.AddModelError(
                    nameof(model.StoneName),
                    "Bu isimde başka bir taş zaten kayıtlı.");

                return View(model);
            }

            string? oldImageUrl = stone.ImageUrl;
            string? newImageUrl = null;

            // Yeni görsel seçilmişse değiştirir
            if (model.ImageFile != null &&
                model.ImageFile.Length > 0)
            {
                var imageValidationError =
                    ValidateImage(model.ImageFile);

                if (imageValidationError != null)
                {
                    model.CurrentImageUrl = stone.ImageUrl;

                    ModelState.AddModelError(
                        nameof(model.ImageFile),
                        imageValidationError);

                    return View(model);
                }

                // Önce yeni görsel kaydedilir
                newImageUrl =
                    await SaveImageAsync(model.ImageFile);

                // Entity yeni görsele geçirilir
                stone.ImageUrl = newImageUrl;
            }

            // Diğer taş bilgilerini günceller
            stone.StoneName = stoneName;

            stone.Color =
                string.IsNullOrWhiteSpace(model.Color)
                    ? null
                    : model.Color.Trim();

            stone.PurchasePrice = model.PurchasePrice;
            stone.SalePrice = model.SalePrice;
            stone.IsActive = model.IsActive;

            // Web sitesi açıklamasını günceller
            stone.WebDescription =
                string.IsNullOrWhiteSpace(model.WebDescription)
                    ? null
                    : model.WebDescription.Trim();

            // Web sitesinde gösterilme durumunu günceller
            stone.ShowOnWeb = model.ShowOnWeb;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                DeleteImage(newImageUrl);
                model.CurrentImageUrl = oldImageUrl;

                ModelState.AddModelError(
                    string.Empty,
                    "Taş güncellenirken bir hata oluştu.");

                return View(model);
            }

            // Veritabanı yeni yolu başarıyla kaydettikten sonra eski görseli siler
            if (newImageUrl != null)
            {
                DeleteImage(oldImageUrl);
            }

            // Taş güncelleme işlemini işlem geçmişine kaydeder
            await _activityLogService.LogAsync(
                "Stone",
                "Update",
                stone.StoneId,
                $"{stone.StoneName} adlı taşın bilgileri güncellendi.");

            return RedirectToAction(nameof(Index));
        }

        // Taşı pasif hale getirir
        [Authorize(Roles = "Admin,Manager")]
        // Geçmiş kayıtlarda kullanıldığı için fiziksel olarak silmez
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var stone = await _context.Stones
                .FirstOrDefaultAsync(x => x.StoneId == id);

            if (stone == null)
            {
                return NotFound();
            }

            // Zaten pasifse tekrar işlem yapmaz
            if (!stone.IsActive)
            {
                return RedirectToAction(nameof(Index));
            }

            stone.IsActive = false;

            await _context.SaveChangesAsync();

            // Taşın pasife alınmasını işlem geçmişine kaydeder
            await _activityLogService.LogAsync(
                "Stone",
                "StatusChange",
                stone.StoneId,
                $"{stone.StoneName} adlı taş pasif duruma getirildi.");

            return RedirectToAction(nameof(Index));
        }

        // Pasif taşı tekrar aktif hale getirir
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            var stone = await _context.Stones
                .FirstOrDefaultAsync(x => x.StoneId == id);

            if (stone == null)
            {
                return NotFound();
            }

            // Zaten aktifse tekrar işlem yapmaz
            if (stone.IsActive)
            {
                return RedirectToAction(nameof(Index));
            }

            stone.IsActive = true;

            await _context.SaveChangesAsync();

            // Taşın tekrar aktif edilmesini işlem geçmişine kaydeder
            await _activityLogService.LogAsync(
                "Stone",
                "StatusChange",
                stone.StoneId,
                $"{stone.StoneName} adlı taş tekrar aktif duruma getirildi.");

            return RedirectToAction(nameof(Index));
        }

        // Yüklenen görselin uzantısını ve boyutunu kontrol eder
        private string? ValidateImage(IFormFile imageFile)
        {
            const long maxFileSize = 5 * 1024 * 1024;

            return ImageUploadValidator.Validate(
                imageFile,
                maxFileSize);
        }

        // Görseli wwwroot/images/stones klasörüne kaydeder
        private async Task<string> SaveImageAsync(
            IFormFile imageFile)
        {
            var extension = Path
                .GetExtension(imageFile.FileName)
                .ToLowerInvariant();

            // Her görsele benzersiz dosya adı verir
            var fileName =
                $"{Guid.NewGuid()}{extension}";

            var folderPath = Path.Combine(
                _webHostEnvironment.WebRootPath,
                "images",
                "stones");

            // Klasör yoksa otomatik oluşturur
            Directory.CreateDirectory(folderPath);

            var filePath = Path.Combine(
                folderPath,
                fileName);

            // Görseli fiziksel olarak kaydeder
            await using var stream =
                new FileStream(
                    filePath,
                    FileMode.CreateNew);

            await imageFile.CopyToAsync(stream);

            // SQL'e kaydedilecek web yolunu döndürür
            return $"/images/stones/{fileName}";
        }

        // Eski taş görselini fiziksel olarak siler
        private void DeleteImage(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return;
            }

            const string stoneImagePrefix = "/images/stones/";

            if (!imageUrl.StartsWith(
                    stoneImagePrefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var relativePath = imageUrl
                .TrimStart('/')
                .Replace(
                    '/',
                    Path.DirectorySeparatorChar);

            var filePath = Path.Combine(
                _webHostEnvironment.WebRootPath,
                relativePath);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
    }
}
