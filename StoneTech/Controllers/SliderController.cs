
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoneTech.Data;
using StoneTech.Entities;
using StoneTech.Models.Slider;
using StoneTech.Services;

namespace StoneTech.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SliderController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        // Maksimum görsel boyutu 5 MB
        private const long MaxImageSize = 5 * 1024 * 1024;

        public SliderController(
            AppDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }


        // Slider kayıtlarını arama ve sayfalama ile listeler
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            int page = 1,
            int pageSize = 10)
        {
            // Sayfa numarasını kontrol eder
            if (page < 1)
            {
                page = 1;
            }

            // Sayfa başına kayıt sayısını kontrol eder
            if (pageSize != 5 &&
                pageSize != 10 &&
                pageSize != 20 &&
                pageSize != 50)
            {
                pageSize = 10;
            }

            // Slider sorgusunu oluşturur
            var query = _context.Sliders
                .AsNoTracking()
                .AsQueryable();

            // Başlığa veya açıklamaya göre arama yapar
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

            // Sayfa numarasını mevcut kayıt sayısına göre sınırlar
            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            // Slider kayıtlarını sıralar ve sayfalar
            var sliders = await query
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

            return View(sliders);
        }


        // Yeni slider ekleme sayfasını açar
        [HttpGet]
        public IActionResult Create()
        {
            return View(new SliderCreateViewModel());
        }

        // Yeni slider kaydını oluşturur
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SliderCreateViewModel model)
        {
            // Görsel seçilip seçilmediğini kontrol eder
            if (model.ImageFile == null)
            {
                ModelState.AddModelError(
                    nameof(model.ImageFile),
                    "Slider görseli seçmelisiniz.");
            }
            else
            {
                // Görseli mevcut doğrulama servisiyle kontrol eder
                string? imageError = ImageUploadValidator.Validate(
                    model.ImageFile,
                    MaxImageSize);

                if (imageError != null)
                {
                    ModelState.AddModelError(
                        nameof(model.ImageFile),
                        imageError);
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? imageUrl = null;

            try
            {
                // Görseli sunucuya kaydeder
                imageUrl = await SaveImageAsync(model.ImageFile!);

                // Yeni slider nesnesini oluşturur
                var slider = new Slider
                {
                    Title = model.Title.Trim(),
                    Description = model.Description?.Trim(),
                    ImageUrl = imageUrl,
                    DisplayOrder = model.DisplayOrder,
                    IsActive = model.IsActive
                };

                _context.Sliders.Add(slider);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Slider başarıyla eklendi.";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                // Kayıt başarısızsa yüklenen görseli temizler
                DeleteImage(imageUrl);

                ModelState.AddModelError(
                    string.Empty,
                    "Slider kaydedilirken bir hata oluştu.");

                return View(model);
            }
        }

        // Slider detaylarını gösterir
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var slider = await _context.Sliders
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (slider == null)
            {
                return NotFound();
            }

            return View(slider);
        }

        // Slider düzenleme sayfasını açar
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var slider = await _context.Sliders
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (slider == null)
            {
                return NotFound();
            }

            // Entity bilgilerini düzenleme modeline aktarır
            var model = new SliderEditViewModel
            {
                Id = slider.Id,
                Title = slider.Title,
                Description = slider.Description,
                CurrentImageUrl = slider.ImageUrl,
                DisplayOrder = slider.DisplayOrder,
                IsActive = slider.IsActive
            };

            return View(model);
        }

        // Slider bilgilerini günceller
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SliderEditViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var slider = await _context.Sliders
                .FirstOrDefaultAsync(x => x.Id == id);

            if (slider == null)
            {
                return NotFound();
            }

            // Yeni görsel seçilmişse doğrular
            if (model.ImageFile != null)
            {
                string? imageError = ImageUploadValidator.Validate(
                    model.ImageFile,
                    MaxImageSize);

                if (imageError != null)
                {
                    ModelState.AddModelError(
                        nameof(model.ImageFile),
                        imageError);
                }
            }

            if (!ModelState.IsValid)
            {
                model.CurrentImageUrl = slider.ImageUrl;

                return View(model);
            }

            string? newImageUrl = null;
            string oldImageUrl = slider.ImageUrl;

            try
            {
                // Yeni görsel seçilmişse sunucuya kaydeder
                if (model.ImageFile != null)
                {
                    newImageUrl = await SaveImageAsync(model.ImageFile);
                }

                // Slider bilgilerini günceller
                slider.Title = model.Title.Trim();
                slider.Description = model.Description?.Trim();
                slider.DisplayOrder = model.DisplayOrder;
                slider.IsActive = model.IsActive;

                // Yeni görsel varsa yolunu günceller
                if (newImageUrl != null)
                {
                    slider.ImageUrl = newImageUrl;
                }

                await _context.SaveChangesAsync();

                // Güncelleme başarılıysa eski görseli siler
                if (newImageUrl != null)
                {
                    TryDeleteImage(oldImageUrl);
                }

                TempData["SuccessMessage"] = "Slider başarıyla güncellendi.";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                // Güncelleme başarısızsa yeni görseli temizler
                TryDeleteImage(newImageUrl);

                model.CurrentImageUrl = oldImageUrl;

                ModelState.AddModelError(
                    string.Empty,
                    "Slider güncellenirken bir hata oluştu.");

                return View(model);
            }
        }

        // Slider silme onay sayfasını açar
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var slider = await _context.Sliders
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (slider == null)
            {
                return NotFound();
            }

            return View(slider);
        }

        // Slider kaydını siler
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var slider = await _context.Sliders
                .FirstOrDefaultAsync(x => x.Id == id);

            if (slider == null)
            {
                return NotFound();
            }

            string imageUrl = slider.ImageUrl;

            try
            {
                // Slider kaydını veritabanından siler
                _context.Sliders.Remove(slider);

                await _context.SaveChangesAsync();

                // Kayıt silindikten sonra görseli temizler
                TryDeleteImage(imageUrl);

                TempData["SuccessMessage"] = "Slider başarıyla silindi.";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                TempData["ErrorMessage"] = "Slider silinirken bir hata oluştu.";

                return RedirectToAction(nameof(Index));
            }
        }

        // Görseli sunucudaki slider klasörüne kaydeder
        private async Task<string> SaveImageAsync(IFormFile file)
        {
            string webRoot = _environment.WebRootPath
                ?? Path.Combine(_environment.ContentRootPath, "wwwroot");

            string folderPath = Path.Combine(
                webRoot,
                "uploads",
                "sliders");

            Directory.CreateDirectory(folderPath);

            // Görsel için benzersiz dosya adı oluşturur
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
                // Kısmen oluşmuş dosyayı temizler
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                throw;
            }

            // Veritabanında tutulacak görsel yolunu döndürür
            return $"/uploads/sliders/{fileName}";
        }

        // Görseli slider klasöründen siler
        private void DeleteImage(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return;
            }

            // Yalnızca slider klasöründeki dosyaları kabul eder
            const string sliderPath = "/uploads/sliders/";

            if (!imageUrl.StartsWith(
                    sliderPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string fileName = imageUrl[sliderPath.Length..];

            // Alt klasör veya farklı dosya yolu kullanımını engeller
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
                "sliders",
                fileName);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        // Görsel silme hatasının kayıt işlemini etkilemesini engeller
        private void TryDeleteImage(string? imageUrl)
        {
            try
            {
                DeleteImage(imageUrl);
            }
            catch (IOException)
            {
                // Görsel silinemezse veritabanı işlemi korunur
            }
            catch (UnauthorizedAccessException)
            {
                // Dosya erişim hatası veritabanı işlemini etkilemez
            }
        }
    }
}
