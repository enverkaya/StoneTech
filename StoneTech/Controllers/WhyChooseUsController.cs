
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoneTech.Data;
using StoneTech.Models.WhyChooseUs;
using WhyChooseUsEntity = StoneTech.Entities.WhyChooseUs;

namespace StoneTech.Controllers
{
    [Authorize(Roles = "Admin")]
    public class WhyChooseUsController : Controller
    {
        private readonly AppDbContext _context;

        // Web sitesinde kullanılacak maksimum özellik sayısı
        private const int MaxItemCount = 6;

        public WhyChooseUsController(AppDbContext context)
        {
            _context = context;
        }

        // Neden Biz özelliklerini listeler
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

            // Özellikleri sorgular
            var query = _context.WhyChooseUsItems
                .AsNoTracking()
                .AsQueryable();

            // Başlık ve açıklama üzerinden arama yapar
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

            // Sayfa numarasını mevcut aralığa getirir
            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            // Özellikleri sıralayarak getirir
            var items = await query
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Sayfalama bilgilerini View'a gönderir
            ViewBag.Search = search ?? "";
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            // Yeni kayıt eklenip eklenemeyeceğini belirler
            ViewBag.CanCreate =
                await _context.WhyChooseUsItems.CountAsync() < MaxItemCount;

            return View(items);
        }

        // Yeni özellik ekleme sayfasını açar
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create()
        {
            // Maksimum özellik sayısını kontrol eder
            int count = await _context.WhyChooseUsItems.CountAsync();

            if (count >= MaxItemCount)
            {
                TempData["ErrorMessage"] =
                    "En fazla 6 adet Neden Biz özelliği ekleyebilirsiniz.";

                return RedirectToAction(nameof(Index));
            }

            // Yeni özelliğe varsayılan sıra numarası verir
            int nextOrder = await _context.WhyChooseUsItems
                .MaxAsync(x => (int?)x.DisplayOrder) ?? 0;

            var model = new WhyChooseUsCreateViewModel
            {
                DisplayOrder = nextOrder + 1,
                IsActive = true
            };

            return View(model);
        }

        // Yeni özelliği kaydeder
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create(
            WhyChooseUsCreateViewModel model)
        {
            // Maksimum özellik sayısını kontrol eder
            int count = await _context.WhyChooseUsItems.CountAsync();

            if (count >= MaxItemCount)
            {
                TempData["ErrorMessage"] =
                    "En fazla 6 adet Neden Biz özelliği ekleyebilirsiniz.";

                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Yeni özelliği oluşturur
                var item = new WhyChooseUsEntity
                {
                    Title = model.Title.Trim(),
                    Description = model.Description?.Trim(),
                    IconClass = model.IconClass?.Trim(),
                    DisplayOrder = model.DisplayOrder,
                    IsActive = model.IsActive
                };

                _context.WhyChooseUsItems.Add(item);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Neden Biz özelliği başarıyla eklendi.";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Özellik eklenirken bir hata oluştu.");

                return View(model);
            }
        }

        // Özelliğin detaylarını gösterir
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var item = await _context.WhyChooseUsItems
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        // Özellik düzenleme sayfasını açar
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.WhyChooseUsItems
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (item == null)
            {
                return NotFound();
            }

            // Mevcut bilgileri düzenleme modeline aktarır
            var model = new WhyChooseUsEditViewModel
            {
                Id = item.Id,
                Title = item.Title,
                Description = item.Description,
                IconClass = item.IconClass,
                DisplayOrder = item.DisplayOrder,
                IsActive = item.IsActive
            };

            return View(model);
        }

        // Özellik bilgilerini günceller
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(
            int id,
            WhyChooseUsEditViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            // Güncellenecek özelliği getirir
            var item = await _context.WhyChooseUsItems
                .FirstOrDefaultAsync(x => x.Id == id);

            if (item == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Özellik bilgilerini günceller
                item.Title = model.Title.Trim();
                item.Description = model.Description?.Trim();
                item.IconClass = model.IconClass?.Trim();
                item.DisplayOrder = model.DisplayOrder;
                item.IsActive = model.IsActive;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Neden Biz özelliği başarıyla güncellendi.";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Özellik güncellenirken bir hata oluştu.");

                return View(model);
            }
        }

        // Özelliği doğrudan siler
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            // Silinecek özelliği getirir
            var item = await _context.WhyChooseUsItems
                .FirstOrDefaultAsync(x => x.Id == id);

            if (item == null)
            {
                return NotFound();
            }

            try
            {
                // Özelliği veritabanından siler
                _context.WhyChooseUsItems.Remove(item);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Neden Biz özelliği başarıyla silindi.";
            }
            catch
            {
                TempData["ErrorMessage"] =
                    "Özellik silinirken bir hata oluştu.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
