using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoneTech.Data;
using StoneTech.Entities;
using StoneTech.Models.Supplier;
using StoneTech.Services;

namespace StoneTech.Controllers
{
    // Tedarikçi modülüne Admin, Manager, Sales ve Accountant rolleri erişebilir
    [Authorize(Roles = "Admin,Manager,Sales,Accountant")]
    public class SupplierController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogService _activityLogService;

        // Veritabanı ve işlem geçmişi servisleri alınır
        public SupplierController(
            AppDbContext context,
            IActivityLogService activityLogService)
        {
            _context = context;
            _activityLogService = activityLogService;
        }

        // Tedarikçileri listeler
        // Arama ve sayfalama işlemlerini yapar
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

            // Tedarikçi sorgusunu oluşturur
            var query = _context.Suppliers
                .AsNoTracking()
                .AsQueryable();

            // Arama yapılmışsa ad, telefon veya adres içinde arar
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.SupplierName.Contains(search) ||
                    (x.Phone != null && x.Phone.Contains(search)) ||
                    (x.Address != null && x.Address.Contains(search)));
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

            // Tedarikçileri ViewModel'e dönüştürür
            var suppliers = await query
                .OrderBy(x => x.SupplierName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new SupplierListViewModel
                {
                    SupplierId = x.SupplierId,
                    SupplierName = x.SupplierName,
                    Phone = x.Phone,
                    Address = x.Address,
                    Note = x.Note,
                    IsActive = x.IsActive
                })
                .ToListAsync();

            // Index View'inin arama ve pagination bilgileri
            ViewBag.Search = search ?? "";
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View(suppliers);
        }

        // Yeni tedarikçi ekleme sayfasını açar
        [Authorize(Roles = "Admin,Manager,Sales")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // Yeni tedarikçiyi veritabanına kaydeder
        [Authorize(Roles = "Admin,Manager,Sales")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            SupplierCreateViewModel model)
        {
            // ViewModel validation hatası varsa sayfaya geri döner
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Tedarikçi adındaki gereksiz boşlukları temizler
            var supplierName = model.SupplierName.Trim();

            // Aynı isimde tedarikçi var mı kontrol eder
            var supplierExists = await _context.Suppliers
                .AnyAsync(x => x.SupplierName == supplierName);

            if (supplierExists)
            {
                ModelState.AddModelError(
                    nameof(model.SupplierName),
                    "Bu isimde bir tedarikçi zaten kayıtlı.");

                return View(model);
            }

            // ViewModel'den Supplier Entity oluşturulur
            var supplier = new Supplier
            {
                SupplierName = supplierName,

                Phone = string.IsNullOrWhiteSpace(model.Phone)
                    ? null
                    : model.Phone.Trim(),

                Address = string.IsNullOrWhiteSpace(model.Address)
                    ? null
                    : model.Address.Trim(),

                Note = string.IsNullOrWhiteSpace(model.Note)
                    ? null
                    : model.Note.Trim(),

                IsActive = true
            };

            _context.Suppliers.Add(supplier);

            await _context.SaveChangesAsync();

            // Tedarikçi oluşturma işlemini işlem geçmişine kaydeder
            await _activityLogService.LogAsync(
                "Supplier",
                "Create",
                supplier.SupplierId,
                $"{supplier.SupplierName} adlı tedarikçi oluşturuldu.");

            return RedirectToAction(nameof(Index));
        }

        // Tedarikçi düzenleme sayfasını açar
        [Authorize(Roles = "Admin,Manager,Sales")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var supplier = await _context.Suppliers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.SupplierId == id);

            // Böyle bir tedarikçi yoksa 404 döner
            if (supplier == null)
            {
                return NotFound();
            }

            // Entity bilgilerini ViewModel'e aktarır
            var model = new SupplierEditViewModel
            {
                SupplierId = supplier.SupplierId,
                SupplierName = supplier.SupplierName,
                Phone = supplier.Phone,
                Address = supplier.Address,
                Note = supplier.Note,
                IsActive = supplier.IsActive
            };

            return View(model);
        }

        // Düzenlenen tedarikçiyi veritabanında günceller
        [Authorize(Roles = "Admin,Manager,Sales")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            SupplierEditViewModel model)
        {
            // ViewModel validation hatası varsa sayfaya geri döner
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Güncellenecek tedarikçiyi bulur
            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(
                    x => x.SupplierId == model.SupplierId);

            if (supplier == null)
            {
                return NotFound();
            }

            // Tedarikçi adındaki gereksiz boşlukları temizler
            var supplierName = model.SupplierName.Trim();

            // Başka bir tedarikçi aynı ismi kullanıyor mu kontrol eder
            var supplierExists = await _context.Suppliers
                .AnyAsync(x =>
                    x.SupplierId != model.SupplierId &&
                    x.SupplierName == supplierName);

            if (supplierExists)
            {
                ModelState.AddModelError(
                    nameof(model.SupplierName),
                    "Bu isimde başka bir tedarikçi zaten kayıtlı.");

                return View(model);
            }

            // ViewModel'den gelen değerleri Entity'ye aktarır
            supplier.SupplierName = supplierName;

            supplier.Phone =
                string.IsNullOrWhiteSpace(model.Phone)
                    ? null
                    : model.Phone.Trim();

            supplier.Address =
                string.IsNullOrWhiteSpace(model.Address)
                    ? null
                    : model.Address.Trim();

            supplier.Note =
                string.IsNullOrWhiteSpace(model.Note)
                    ? null
                    : model.Note.Trim();

            supplier.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            // Tedarikçi güncelleme işlemini işlem geçmişine kaydeder
            await _activityLogService.LogAsync(
                "Supplier",
                "Update",
                supplier.SupplierId,
                $"{supplier.SupplierName} adlı tedarikçinin bilgileri güncellendi.");

            return RedirectToAction(nameof(Index));
        }

        // Tedarikçiyi pasif hale getirir
        // Geçmiş alım kayıtlarında kullanılabileceği için fiziksel olarak silmez
        // Tedarikçi durumunu yalnızca Admin ve Manager değiştirebilir
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(x => x.SupplierId == id);

            if (supplier == null)
            {
                return NotFound();
            }

            // Zaten pasifse tekrar işlem yapmaz
            if (!supplier.IsActive)
            {
                return RedirectToAction(nameof(Index));
            }

            supplier.IsActive = false;

            await _context.SaveChangesAsync();

            // Tedarikçinin pasife alınmasını işlem geçmişine kaydeder
            await _activityLogService.LogAsync(
                "Supplier",
                "StatusChange",
                supplier.SupplierId,
                $"{supplier.SupplierName} adlı tedarikçi pasif duruma getirildi.");

            return RedirectToAction(nameof(Index));
        }

        // Pasif tedarikçiyi tekrar aktif hale getirir
        // Tedarikçi durumunu yalnızca Admin ve Manager değiştirebilir
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(x => x.SupplierId == id);

            if (supplier == null)
            {
                return NotFound();
            }

            // Zaten aktifse tekrar işlem yapmaz
            if (supplier.IsActive)
            {
                return RedirectToAction(nameof(Index));
            }

            supplier.IsActive = true;

            await _context.SaveChangesAsync();

            // Tedarikçinin tekrar aktif edilmesini işlem geçmişine kaydeder
            await _activityLogService.LogAsync(
                "Supplier",
                "StatusChange",
                supplier.SupplierId,
                $"{supplier.SupplierName} adlı tedarikçi tekrar aktif duruma getirildi.");

            return RedirectToAction(nameof(Index));
        }
    }
}