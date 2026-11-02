
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoneTech.Data;
using StoneTech.Entities;
using StoneTech.Models.Service;
using StoneTech.Services;

namespace StoneTech.Controllers
{
    [Authorize(Roles = "Admin")]
    [Authorize]
    public class ServiceController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        // Görsel için maksimum dosya boyutu 5 MB
        private const long MaxImageSize = 5 * 1024 * 1024;

        public ServiceController(
            AppDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // Hizmetleri arama ve sayfalama ile listeler
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

            // Hizmet sorgusunu oluşturur
            var query = _context.Services
                .AsNoTracking()
                .AsQueryable();

            // Başlık ve açıklamaya göre arama yapar
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x =>
                    x.Title.Contains(search) ||
                    (x.Description != null &&
                     x.Description.Contains(search)));
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

            // Hizmetleri sıralayarak getirir
            var services = await query
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

            return View(services);
        }

        // Yeni hizmet ekleme sayfasını açar
        [Authorize(Roles = "Admin,Manager")]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new ServiceCreateViewModel());
        }

        // Yeni hizmet kaydını oluşturur
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceCreateViewModel model)
        {
            // Hizmet görselini kontrol eder
            if (model.ImageFile == null)
            {
                ModelState.AddModelError(
                    nameof(model.ImageFile),
                    "Hizmet görseli seçmelisiniz.");
            }
            else
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
                return View(model);
            }

            string? uploadedImageUrl = null;

            try
            {
                // Hizmet görselini kaydeder
                uploadedImageUrl = await SaveImageAsync(model.ImageFile!);

                // Yeni hizmeti oluşturur
                var service = new Service
                {
                    Title = model.Title.Trim(),
                    Description = model.Description.Trim(),
                    ImageUrl = uploadedImageUrl,
                    DisplayOrder = model.DisplayOrder,
                    IsActive = model.IsActive
                };

                // Hizmeti veritabanına kaydeder
                _context.Services.Add(service);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Hizmet başarıyla eklendi.";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                // Hata durumunda yüklenen görseli temizler
                TryDeleteImage(uploadedImageUrl);

                ModelState.AddModelError(
                    string.Empty,
                    "Hizmet kaydedilirken bir hata oluştu.");

                return View(model);
            }
        }

        // Hizmet detaylarını admin panelinde gösterir
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var service = await _context.Services
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (service == null)
            {
                return NotFound();
            }

            return View(service);
        }

        // Hizmet düzenleme sayfasını açar
        [Authorize(Roles = "Admin,Manager")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var service = await _context.Services
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (service == null)
            {
                return NotFound();
            }

            // Hizmet bilgilerini düzenleme modeline aktarır
            var model = new ServiceEditViewModel
            {
                Id = service.Id,
                Title = service.Title,
                Description = service.Description ?? string.Empty,
                CurrentImageUrl = service.ImageUrl,
                DisplayOrder = service.DisplayOrder,
                IsActive = service.IsActive
            };

            return View(model);
        }

        // Hizmet bilgilerini günceller
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            ServiceEditViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            // Düzenlenecek hizmeti getirir
            var service = await _context.Services
                .FirstOrDefaultAsync(x => x.Id == id);

            if (service == null)
            {
                return NotFound();
            }

            // Mevcut görseli modele aktarır
            model.CurrentImageUrl = service.ImageUrl;

            // Yeni görsel seçilmişse kontrol eder
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
                return View(model);
            }

            string? newImageUrl = null;
            string? oldImageUrl = service.ImageUrl;

            try
            {
                // Yeni görsel seçilmişse kaydeder
                if (model.ImageFile != null)
                {
                    newImageUrl = await SaveImageAsync(model.ImageFile);
                }

                // Hizmet bilgilerini günceller
                service.Title = model.Title.Trim();
                service.Description = model.Description.Trim();
                service.DisplayOrder = model.DisplayOrder;
                service.IsActive = model.IsActive;

                // Yeni görsel varsa eskisinin yerine kullanır
                if (newImageUrl != null)
                {
                    service.ImageUrl = newImageUrl;
                }

                await _context.SaveChangesAsync();

                // Güncelleme başarılıysa eski görseli temizler
                if (newImageUrl != null)
                {
                    TryDeleteImage(oldImageUrl);
                }

                TempData["SuccessMessage"] =
                    "Hizmet başarıyla güncellendi.";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                // Hata durumunda yeni görseli temizler
                TryDeleteImage(newImageUrl);

                model.CurrentImageUrl = oldImageUrl;

                ModelState.AddModelError(
                    string.Empty,
                    "Hizmet güncellenirken bir hata oluştu.");

                return View(model);
            }
        }

        // Hizmeti ve ilişkili eski görsellerini siler
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var service = await _context.Services
                .Include(x => x.ServiceImages)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (service == null)
            {
                return NotFound();
            }

            // Hizmete ait tüm dosya yollarını saklar
            var imageUrls = service.ServiceImages
                .Select(x => x.ImageUrl)
                .ToList();

            if (!string.IsNullOrWhiteSpace(service.ImageUrl))
            {
                imageUrls.Add(service.ImageUrl);
            }

            try
            {
                // Hizmeti siler, ilişkili kayıtlar cascade ile silinir
                _context.Services.Remove(service);

                await _context.SaveChangesAsync();

                // Veritabanı işlemi başarılıysa dosyaları temizler
                foreach (string imageUrl in imageUrls.Distinct())
                {
                    TryDeleteImage(imageUrl);
                }

                TempData["SuccessMessage"] =
                    "Hizmet başarıyla silindi.";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                TempData["ErrorMessage"] =
                    "Hizmet silinirken bir hata oluştu.";

                return RedirectToAction(nameof(Index));
            }
        }

        // Görseli hizmetler klasörüne kaydeder
        private async Task<string> SaveImageAsync(IFormFile file)
        {
            string webRoot = _environment.WebRootPath
                ?? Path.Combine(_environment.ContentRootPath, "wwwroot");

            string folderPath = Path.Combine(
                webRoot,
                "uploads",
                "services");

            Directory.CreateDirectory(folderPath);

            // Benzersiz dosya adı oluşturur
            string extension = Path
                .GetExtension(file.FileName)
                .ToLowerInvariant();

            string fileName = $"{Guid.NewGuid():N}{extension}";

            string filePath = Path.Combine(folderPath, fileName);

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
                // Eksik yüklenmiş dosyayı temizler
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                throw;
            }

            // Veritabanında saklanacak görsel yolunu döndürür
            return $"/uploads/services/{fileName}";
        }

        // Yalnızca hizmetler klasöründeki görselleri siler
        private void DeleteImageFile(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return;
            }

            const string servicePath = "/uploads/services/";

            if (!imageUrl.StartsWith(
                    servicePath,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string fileName = imageUrl[servicePath.Length..];

            if (string.IsNullOrWhiteSpace(fileName) ||
                fileName != Path.GetFileName(fileName))
            {
                return;
            }

            string webRoot = _environment.WebRootPath
                ?? Path.Combine(_environment.ContentRootPath, "wwwroot");

            string filePath = Path.Combine(
                webRoot,
                "uploads",
                "services",
                fileName);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        // Dosya silme hatalarının veritabanı işlemini etkilemesini önler
        private void TryDeleteImage(string? imageUrl)
        {
            try
            {
                DeleteImageFile(imageUrl);
            }
            catch (IOException)
            {
                // Dosya silme hatası işlemi etkilemez
            }
            catch (UnauthorizedAccessException)
            {
                // Dosya erişim hatası işlemi etkilemez
            }
        }
    }
}
