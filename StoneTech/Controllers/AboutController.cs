
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoneTech.Data;
using StoneTech.Models.About;
using StoneTech.Services;
using AboutEntity = StoneTech.Entities.About;

namespace StoneTech.Controllers
{
    [Authorize(Roles = "Admin")]
    [Authorize]
    public class AboutController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        // Görseller için maksimum dosya boyutu
        private const long MaxImageSize = 5 * 1024 * 1024;

        public AboutController(
            AppDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // Hakkımızda kayıtlarını listeler
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var abouts = await _context.Abouts
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .ToListAsync();

            return View(abouts);
        }

        // Yeni hakkımızda kaydı oluşturma sayfasını açar
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create()
        {
            // Hakkımızda kaydının daha önce oluşturulup oluşturulmadığını kontrol eder
            bool exists = await _context.Abouts.AnyAsync();

            if (exists)
            {
                TempData["ErrorMessage"] =
                    "Hakkımızda kaydı zaten mevcut. Mevcut kaydı düzenleyebilirsiniz.";

                return RedirectToAction(nameof(Index));
            }

            return View(new AboutCreateViewModel());
        }

        // Yeni hakkımızda kaydını kaydeder
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create(
            AboutCreateViewModel model)
        {
            // Birden fazla hakkımızda kaydı oluşturulmasını engeller
            if (await _context.Abouts.AnyAsync())
            {
                TempData["ErrorMessage"] =
                    "Hakkımızda kaydı zaten mevcut.";

                return RedirectToAction(nameof(Index));
            }

            // Yüklenen görselleri doğrular
            ValidateImage(model.ImageFile1, nameof(model.ImageFile1));
            ValidateImage(model.ImageFile2, nameof(model.ImageFile2));
            ValidateImage(model.ImageFile3, nameof(model.ImageFile3));

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? imageUrl1 = null;
            string? imageUrl2 = null;
            string? imageUrl3 = null;

            try
            {
                // Birinci görseli kaydeder
                if (model.ImageFile1 != null)
                {
                    imageUrl1 = await SaveImageAsync(model.ImageFile1);
                }

                // İkinci görseli kaydeder
                if (model.ImageFile2 != null)
                {
                    imageUrl2 = await SaveImageAsync(model.ImageFile2);
                }

                // Üçüncü görseli kaydeder
                if (model.ImageFile3 != null)
                {
                    imageUrl3 = await SaveImageAsync(model.ImageFile3);
                }

                // Yeni hakkımızda kaydını oluşturur
                var about = new AboutEntity
                {
                    Title = model.Title.Trim(),
                    Description1 = model.Description1?.Trim(),
                    Description2 = model.Description2?.Trim(),
                    ImageUrl1 = imageUrl1,
                    ImageUrl2 = imageUrl2,
                    ImageUrl3 = imageUrl3
                };

                _context.Abouts.Add(about);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Hakkımızda kaydı başarıyla oluşturuldu.";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                // Kayıt başarısızsa yüklenen görselleri temizler
                TryDeleteImage(imageUrl1);
                TryDeleteImage(imageUrl2);
                TryDeleteImage(imageUrl3);

                ModelState.AddModelError(
                    string.Empty,
                    "Hakkımızda kaydı oluşturulurken bir hata oluştu.");

                return View(model);
            }
        }

        // Hakkımızda kaydının detaylarını gösterir
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var about = await _context.Abouts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (about == null)
            {
                return NotFound();
            }

            return View(about);
        }

        // Hakkımızda düzenleme sayfasını açar
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int id)
        {
            var about = await _context.Abouts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (about == null)
            {
                return NotFound();
            }

            // Mevcut bilgileri düzenleme modeline aktarır
            var model = new AboutEditViewModel
            {
                Id = about.Id,
                Title = about.Title,
                Description1 = about.Description1,
                Description2 = about.Description2,
                CurrentImageUrl1 = about.ImageUrl1,
                CurrentImageUrl2 = about.ImageUrl2,
                CurrentImageUrl3 = about.ImageUrl3
            };

            return View(model);
        }

        // Hakkımızda kaydını günceller
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(
            int id,
            AboutEditViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            // Güncellenecek hakkımızda kaydını getirir
            var about = await _context.Abouts
                .FirstOrDefaultAsync(x => x.Id == id);

            if (about == null)
            {
                return NotFound();
            }

            // Yeni seçilen görselleri doğrular
            ValidateImage(model.ImageFile1, nameof(model.ImageFile1));
            ValidateImage(model.ImageFile2, nameof(model.ImageFile2));
            ValidateImage(model.ImageFile3, nameof(model.ImageFile3));

            if (!ModelState.IsValid)
            {
                FillCurrentImages(model, about);

                return View(model);
            }

            // Eski görsel yollarını saklar
            string? oldImageUrl1 = about.ImageUrl1;
            string? oldImageUrl2 = about.ImageUrl2;
            string? oldImageUrl3 = about.ImageUrl3;

            // Yeni görsel yollarını saklar
            string? newImageUrl1 = null;
            string? newImageUrl2 = null;
            string? newImageUrl3 = null;

            try
            {
                // Yeni birinci görsel seçilmişse kaydeder
                if (model.ImageFile1 != null)
                {
                    newImageUrl1 = await SaveImageAsync(model.ImageFile1);
                }

                // Yeni ikinci görsel seçilmişse kaydeder
                if (model.ImageFile2 != null)
                {
                    newImageUrl2 = await SaveImageAsync(model.ImageFile2);
                }

                // Yeni üçüncü görsel seçilmişse kaydeder
                if (model.ImageFile3 != null)
                {
                    newImageUrl3 = await SaveImageAsync(model.ImageFile3);
                }

                // Metin bilgilerini günceller
                about.Title = model.Title.Trim();
                about.Description1 = model.Description1?.Trim();
                about.Description2 = model.Description2?.Trim();

                // Yeni görseller varsa dosya yollarını günceller
                if (newImageUrl1 != null)
                {
                    about.ImageUrl1 = newImageUrl1;
                }

                if (newImageUrl2 != null)
                {
                    about.ImageUrl2 = newImageUrl2;
                }

                if (newImageUrl3 != null)
                {
                    about.ImageUrl3 = newImageUrl3;
                }

                await _context.SaveChangesAsync();

                // Güncellenen görsellerin eski dosyalarını siler
                if (newImageUrl1 != null)
                {
                    TryDeleteImage(oldImageUrl1);
                }

                if (newImageUrl2 != null)
                {
                    TryDeleteImage(oldImageUrl2);
                }

                if (newImageUrl3 != null)
                {
                    TryDeleteImage(oldImageUrl3);
                }

                TempData["SuccessMessage"] =
                    "Hakkımızda kaydı başarıyla güncellendi.";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                // Güncelleme başarısızsa yeni yüklenen görselleri temizler
                TryDeleteImage(newImageUrl1);
                TryDeleteImage(newImageUrl2);
                TryDeleteImage(newImageUrl3);

                FillCurrentImages(model, about);

                ModelState.AddModelError(
                    string.Empty,
                    "Hakkımızda kaydı güncellenirken bir hata oluştu.");

                return View(model);
            }
        }

        // Hakkımızda kaydını doğrudan siler
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var about = await _context.Abouts
                .FirstOrDefaultAsync(x => x.Id == id);

            if (about == null)
            {
                return NotFound();
            }

            // Silinecek görsellerin yollarını saklar
            string? imageUrl1 = about.ImageUrl1;
            string? imageUrl2 = about.ImageUrl2;
            string? imageUrl3 = about.ImageUrl3;

            try
            {
                // Hakkımızda kaydını veritabanından siler
                _context.Abouts.Remove(about);

                await _context.SaveChangesAsync();

                // Veritabanı kaydı silindikten sonra görselleri temizler
                TryDeleteImage(imageUrl1);
                TryDeleteImage(imageUrl2);
                TryDeleteImage(imageUrl3);

                TempData["SuccessMessage"] =
                    "Hakkımızda kaydı başarıyla silindi.";
            }
            catch
            {
                TempData["ErrorMessage"] =
                    "Hakkımızda kaydı silinirken bir hata oluştu.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Yüklenen görseli mevcut doğrulama sınıfıyla kontrol eder
        private void ValidateImage(
            IFormFile? imageFile,
            string propertyName)
        {
            if (imageFile == null)
            {
                return;
            }

            string? error = ImageUploadValidator.Validate(
                imageFile,
                MaxImageSize);

            if (error != null)
            {
                ModelState.AddModelError(propertyName, error);
            }
        }

        // Mevcut görsel yollarını düzenleme modeline aktarır
        private void FillCurrentImages(
            AboutEditViewModel model,
            AboutEntity about)
        {
            model.CurrentImageUrl1 = about.ImageUrl1;
            model.CurrentImageUrl2 = about.ImageUrl2;
            model.CurrentImageUrl3 = about.ImageUrl3;
        }

        // Görseli hakkımızda klasörüne kaydeder
        private async Task<string> SaveImageAsync(IFormFile file)
        {
            string webRoot = _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");

            string folderPath = Path.Combine(
                webRoot,
                "uploads",
                "about");

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
                // Yarım kalan dosyayı temizler
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                throw;
            }

            // Veritabanına kaydedilecek görsel yolunu döndürür
            return $"/uploads/about/{fileName}";
        }

        // Hakkımızda klasöründeki görseli siler
        private void DeleteImageFile(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return;
            }

            const string aboutPath = "/uploads/about/";

            // Yalnızca hakkımızda klasöründeki görselleri kabul eder
            if (!imageUrl.StartsWith(
                aboutPath,
                StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string fileName = imageUrl[aboutPath.Length..];

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
                "about",
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
