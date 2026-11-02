
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoneTech.Data;
using StoneTech.Models.Gallery;
using StoneTech.Services;
using GalleryEntity = StoneTech.Entities.Gallery;

namespace StoneTech.Controllers
{
    [Authorize(Roles = "Admin")]
    [Authorize]
    public class GalleryController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        // Görseller için maksimum dosya boyutu
        private const long MaxImageSize = 5 * 1024 * 1024;

        public GalleryController(
            AppDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // Galeri görsellerini listeler
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

            if (pageSize != 5 &&
                pageSize != 10 &&
                pageSize != 20 &&
                pageSize != 50)
            {
                pageSize = 10;
            }

            // Galeri sorgusunu oluşturur
            var query = _context.Galleries
                .AsNoTracking()
                .AsQueryable();

            // Başlığa göre arama yapar
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x =>
                    x.Title != null &&
                    x.Title.Contains(search));
            }

            // Toplam kayıt sayısını hesaplar
            int totalCount = await query.CountAsync();

            // Toplam sayfa sayısını hesaplar
            int totalPages = (int)Math.Ceiling(
                totalCount / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            // Görselleri sıralayarak getirir
            var galleries = await query
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Sayfalama bilgilerini View'e gönderir
            ViewBag.Search = search ?? "";
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View(galleries);
        }

        // Yeni galeri görseli ekleme sayfasını açar
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Create()
        {
            return View(new GalleryCreateViewModel());
        }

        // Yeni galeri görselini kaydeder
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create(
            GalleryCreateViewModel model)
        {
            // Görselin seçilip seçilmediğini kontrol eder
            if (model.ImageFile == null)
            {
                ModelState.AddModelError(
                    nameof(model.ImageFile),
                    "Galeri görseli seçmelisiniz.");
            }
            else
            {
                // Mevcut görsel doğrulama sınıfını kullanır
                string? error = ImageUploadValidator.Validate(
                    model.ImageFile,
                    MaxImageSize);

                if (error != null)
                {
                    ModelState.AddModelError(
                        nameof(model.ImageFile),
                        error);
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? uploadedImageUrl = null;

            try
            {
                // Görseli sunucuya kaydeder
                uploadedImageUrl = await SaveImageAsync(
                    model.ImageFile!);

                // Yeni galeri kaydını oluşturur
                var gallery = new GalleryEntity
                {
                    Title = model.Title?.Trim(),
                    ImageUrl = uploadedImageUrl,
                    DisplayOrder = model.DisplayOrder,
                    IsActive = model.IsActive
                };

                // Veritabanına kaydeder
                _context.Galleries.Add(gallery);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Galeri görseli başarıyla eklendi.";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                // Kayıt başarısızsa yüklenen görseli temizler
                TryDeleteImage(uploadedImageUrl);

                ModelState.AddModelError(
                    string.Empty,
                    "Galeri görseli kaydedilirken bir hata oluştu.");

                return View(model);
            }
        }

        // Galeri görselinin detaylarını gösterir
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var gallery = await _context.Galleries
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (gallery == null)
            {
                return NotFound();
            }

            return View(gallery);
        }

        // Galeri görseli düzenleme sayfasını açar
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int id)
        {
            var gallery = await _context.Galleries
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (gallery == null)
            {
                return NotFound();
            }

            // Mevcut bilgileri düzenleme modeline aktarır
            var model = new GalleryEditViewModel
            {
                Id = gallery.Id,
                Title = gallery.Title,
                CurrentImageUrl = gallery.ImageUrl,
                DisplayOrder = gallery.DisplayOrder,
                IsActive = gallery.IsActive
            };

            return View(model);
        }

        // Galeri görselini günceller
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(
            int id,
            GalleryEditViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            // Güncellenecek galeri kaydını getirir
            var gallery = await _context.Galleries
                .FirstOrDefaultAsync(x => x.Id == id);

            if (gallery == null)
            {
                return NotFound();
            }

            // Yeni görsel seçilmişse doğrular
            if (model.ImageFile != null)
            {
                string? error = ImageUploadValidator.Validate(
                    model.ImageFile,
                    MaxImageSize);

                if (error != null)
                {
                    ModelState.AddModelError(
                        nameof(model.ImageFile),
                        error);
                }
            }

            if (!ModelState.IsValid)
            {
                model.CurrentImageUrl = gallery.ImageUrl;

                return View(model);
            }

            string? oldImageUrl = gallery.ImageUrl;
            string? newImageUrl = null;

            try
            {
                // Yeni görsel seçilmişse sunucuya kaydeder
                if (model.ImageFile != null)
                {
                    newImageUrl = await SaveImageAsync(
                        model.ImageFile);
                }

                // Galeri bilgilerini günceller
                gallery.Title = model.Title?.Trim();
                gallery.DisplayOrder = model.DisplayOrder;
                gallery.IsActive = model.IsActive;

                // Yeni görsel varsa dosya yolunu günceller
                if (newImageUrl != null)
                {
                    gallery.ImageUrl = newImageUrl;
                }

                await _context.SaveChangesAsync();

                // Güncelleme başarılıysa eski görseli siler
                if (newImageUrl != null)
                {
                    TryDeleteImage(oldImageUrl);
                }

                TempData["SuccessMessage"] =
                    "Galeri görseli başarıyla güncellendi.";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                // Güncelleme başarısızsa yeni görseli temizler
                TryDeleteImage(newImageUrl);

                model.CurrentImageUrl = oldImageUrl;

                ModelState.AddModelError(
                    string.Empty,
                    "Galeri görseli güncellenirken bir hata oluştu.");

                return View(model);
            }
        }

        // Galeri görselini doğrudan siler
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            // Silinecek galeri kaydını getirir
            var gallery = await _context.Galleries
                .FirstOrDefaultAsync(x => x.Id == id);

            if (gallery == null)
            {
                return NotFound();
            }

            // Silinecek görselin yolunu saklar
            string imageUrl = gallery.ImageUrl;

            try
            {
                // Veritabanı kaydını siler
                _context.Galleries.Remove(gallery);

                await _context.SaveChangesAsync();

                // Kayıt silindikten sonra fiziksel görseli temizler
                TryDeleteImage(imageUrl);

                TempData["SuccessMessage"] =
                    "Galeri görseli başarıyla silindi.";
            }
            catch
            {
                TempData["ErrorMessage"] =
                    "Galeri görseli silinirken bir hata oluştu.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Görseli galeri klasörüne kaydeder
        private async Task<string> SaveImageAsync(IFormFile file)
        {
            string webRoot = _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");

            string folderPath = Path.Combine(
                webRoot,
                "uploads",
                "gallery");

            Directory.CreateDirectory(folderPath);

            // Benzersiz dosya adı oluşturur
            string extension = Path
                .GetExtension(file.FileName)
                .ToLowerInvariant();

            string fileName = $"{Guid.NewGuid():N}{extension}";

            string filePath = Path.Combine(
                folderPath,
                fileName);

            try
            {
                // Görseli fiziksel klasöre kaydeder
                await using var stream = new FileStream(
                    filePath,
                    FileMode.CreateNew);

                await file.CopyToAsync(stream);
            }
            catch
            {
                // Eksik yüklenen dosyayı temizler
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                throw;
            }

            // Veritabanında saklanacak yolu döndürür
            return $"/uploads/gallery/{fileName}";
        }

        // Galeri klasöründeki görseli fiziksel olarak siler
        private void DeleteImageFile(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return;
            }

            const string galleryPath = "/uploads/gallery/";

            // Yalnızca galeri klasöründeki dosyaları kabul eder
            if (!imageUrl.StartsWith(
                galleryPath,
                StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string fileName = imageUrl[galleryPath.Length..];

            // Klasör dışına çıkılmasını engeller
            if (string.IsNullOrWhiteSpace(fileName) ||
                fileName != Path.GetFileName(fileName))
            {
                return;
            }

            string webRoot = _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");

            string filePath = Path.Combine(
                webRoot,
                "uploads",
                "gallery",
                fileName);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        // Dosya silme hatalarının işlemi durdurmasını önler
        private void TryDeleteImage(string? imageUrl)
        {
            try
            {
                DeleteImageFile(imageUrl);
            }
            catch (IOException)
            {
                // Dosya silme hatası veritabanı işlemini etkilemez
            }
            catch (UnauthorizedAccessException)
            {
                // Dosya erişim hatası veritabanı işlemini etkilemez
            }
        }
    }
}
