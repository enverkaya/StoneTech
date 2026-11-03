using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoneTech.Data;
using StoneTech.Entities;
using StoneTech.Enums;
using StoneTech.Models.Employee;
using StoneTech.Services;
using Microsoft.AspNetCore.Authorization;
using System.Text;

namespace StoneTech.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EmployeeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IActivityLogService _activityLogService;

        // Profil fotoğrafı için izin verilen maksimum dosya boyutu
        private const long MaxImageSize = 5 * 1024 * 1024;

        // Veritabanı, dosya sistemi ve işlem geçmişi servisleri alınır
        public EmployeeController(
            AppDbContext context,
            IWebHostEnvironment environment,
            IActivityLogService activityLogService)
        {
            _context = context;
            _environment = environment;
            _activityLogService = activityLogService;
        }

        // Personel listesini getirir
        // Arama ve sayfalama işlemlerini gerçekleştirir
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            int page = 1,
            int pageSize = 10)
        {
            // Geçersiz sayfa numarasını düzeltir
            if (page < 1)
            {
                page = 1;
            }

            // Sayfa başına gösterilecek kayıt sayısını sınırlar
            int[] allowedPageSizes = { 10, 25, 50, 100 };

            if (!allowedPageSizes.Contains(pageSize))
            {
                pageSize = 10;
            }

            // Personelleri sorgular
            IQueryable<Employee> query = _context.Employees
                .AsNoTracking()
                .Include(x => x.User);

            // Arama metni girilmişse ilgili alanlarda arama yapar
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x =>
                    x.FullName.Contains(search) ||
                    x.Position.Contains(search) ||
                    (x.Phone != null && x.Phone.Contains(search)) ||
                    (x.Email != null && x.Email.Contains(search)) ||
                    (x.User != null && x.User.Username.Contains(search)));
            }

            // Toplam kayıt sayısını hesaplar
            int totalCount = await query.CountAsync();

            // Toplam sayfa sayısını hesaplar
            int totalPages = (int)Math.Ceiling(
                totalCount / (double)pageSize);

            // Sayfa numarası toplam sayfa sayısından büyükse son sayfaya gider
            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            // Personel listesini ViewModel'e dönüştürür
            List<EmployeeListViewModel> employees = await query
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new EmployeeListViewModel
                {
                    EmployeeId = x.EmployeeId,
                    FullName = x.FullName,
                    Phone = x.Phone,
                    Email = x.Email,
                    Position = x.Position,
                    HireDate = x.HireDate,
                    ImageUrl = x.ImageUrl,
                    IsActive = x.IsActive,
                    HasUserAccount = x.User != null
                })
                .ToListAsync();

            // Sayfalama bilgilerini View'a gönderir
            ViewBag.Search = search;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = totalPages;

            return View(employees);
        }

        // Yeni personel oluşturma ekranını açar
        [HttpGet]
        public IActionResult Create()
        {
            EmployeeCreateViewModel model = new()
            {
                HireDate = DateTime.Now,
                IsActive = true,
            };

            return View(model);
        }

        // Yeni personeli veritabanına kaydeder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeCreateViewModel model)
        {
            // Metin alanlarını düzenler
            model.FullName = model.FullName.Trim();
            model.Position = model.Position.Trim();
            model.Phone = NormalizeNullable(model.Phone);
            model.Email = NormalizeNullable(model.Email);
            model.Note = NormalizeNullable(model.Note);

            // İşe giriş tarihinin gelecekte olmasını engeller
            if (model.HireDate.Date > DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(model.HireDate),
                    "İşe giriş tarihi bugünden ileri bir tarih olamaz.");
            }

            // E-posta adresi girilmişse aynı e-posta ile kayıtlı personeli kontrol eder
            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                bool emailExists = await _context.Employees
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.Email != null &&
                        x.Email == model.Email);

                if (emailExists)
                {
                    ModelState.AddModelError(
                        nameof(model.Email),
                        "Bu e-posta adresi başka bir personelde kullanılıyor.");
                }
            }

            // Fotoğraf dosyasını doğrular
            if (model.ImageFile != null)
            {
                ValidateImage(
                    model.ImageFile,
                    nameof(model.ImageFile));
            }

            // Model doğrulama başarısızsa formu tekrar gösterir
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? imageUrl = null;

            // Profil fotoğrafı seçilmişse sunucuya kaydeder
            if (model.ImageFile != null)
            {
                imageUrl = await SaveImageAsync(model.ImageFile);
            }

            // Yeni personel kaydını oluşturur
            Employee employee = new()
            {
                FullName = model.FullName,
                Phone = model.Phone,
                Email = model.Email,
                Position = model.Position,
                HireDate = model.HireDate.Date,
                TerminationDate = null,
                Note = model.Note,
                CreatedDate = DateTime.Now,
                ImageUrl = imageUrl,
                IsActive = model.IsActive
            };

            _context.Employees.Add(employee);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                DeleteImage(imageUrl);

                ModelState.AddModelError(
                    string.Empty,
                    "Personel kaydedilirken bir hata oluştu.");

                return View(model);
            }

            // Personel oluşturma işlemini işlem geçmişine kaydeder
            await _activityLogService.LogAsync(
                "Employee",
                "Create",
                employee.EmployeeId,
                $"{employee.FullName} adlı personel oluşturuldu.");

            TempData["SuccessMessage"] =
                "Personel başarıyla eklendi.";

            return RedirectToAction(nameof(Index));
        }

        // Personelin detay bilgilerini gösterir
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            // Personeli ve varsa kullanıcı hesabını getirir
            Employee? employee = await _context.Employees
                .AsNoTracking()
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.EmployeeId == id);

            if (employee == null)
            {
                return NotFound();
            }

            // Entity bilgilerini detay ViewModel'ine dönüştürür
            EmployeeDetailsViewModel model = new()
            {
                EmployeeId = employee.EmployeeId,
                FullName = employee.FullName,
                Phone = employee.Phone,
                Email = employee.Email,
                Position = employee.Position,
                HireDate = employee.HireDate,
                TerminationDate = employee.TerminationDate,
                Note = employee.Note,
                CreatedDate = employee.CreatedDate,
                ImageUrl = employee.ImageUrl,
                IsActive = employee.IsActive,

                HasUserAccount = employee.User != null,
                Username = employee.User?.Username,
                Role = employee.User?.Role,
                IsUserActive = employee.User?.IsActive,
                LastLoginDate = employee.User?.LastLoginDate
            };

            return View(model);
        }

        // Personel düzenleme ekranını açar
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            // Düzenlenecek personeli getirir
            Employee? employee = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.EmployeeId == id);

            if (employee == null)
            {
                return NotFound();
            }

            // Entity bilgilerini düzenleme ViewModel'ine dönüştürür
            EmployeeEditViewModel model = new()
            {
                EmployeeId = employee.EmployeeId,
                FullName = employee.FullName,
                Phone = employee.Phone,
                Email = employee.Email,
                Position = employee.Position,
                HireDate = employee.HireDate,
                TerminationDate = employee.TerminationDate,
                Note = employee.Note,
                CurrentImageUrl = employee.ImageUrl,
                IsActive = employee.IsActive
            };

            return View(model);
        }

        // Personel bilgilerini günceller
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            EmployeeEditViewModel model)
        {
            // Düzenlenecek personeli kullanıcı hesabıyla birlikte getirir
            Employee? employee = await _context.Employees
                .Include(x => x.User)
                .FirstOrDefaultAsync(
                    x => x.EmployeeId == model.EmployeeId);

            if (employee == null)
            {
                return NotFound();
            }

            // Mevcut fotoğraf yolunu güvenilir şekilde veritabanından alır
            model.CurrentImageUrl = employee.ImageUrl;

            // Metin alanlarını düzenler
            model.FullName = model.FullName.Trim();
            model.Position = model.Position.Trim();
            model.Phone = NormalizeNullable(model.Phone);
            model.Email = NormalizeNullable(model.Email);
            model.Note = NormalizeNullable(model.Note);

            // İşe giriş tarihinin gelecekte olmasını engeller
            if (model.HireDate.Date > DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(model.HireDate),
                    "İşe giriş tarihi bugünden ileri bir tarih olamaz.");
            }

            // İşten ayrılma tarihinin işe giriş tarihinden önce olmasını engeller
            if (model.TerminationDate.HasValue &&
                model.TerminationDate.Value.Date <
                model.HireDate.Date)
            {
                ModelState.AddModelError(
                    nameof(model.TerminationDate),
                    "İşten ayrılma tarihi işe giriş tarihinden önce olamaz.");
            }

            // İşten ayrılma tarihinin gelecekte olmasını engeller
            if (model.TerminationDate.HasValue &&
                model.TerminationDate.Value.Date >
                DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(model.TerminationDate),
                    "İşten ayrılma tarihi bugünden ileri bir tarih olamaz.");
            }

            // E-posta adresi girilmişse başka personelde kullanılıp kullanılmadığını kontrol eder
            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                bool emailExists = await _context.Employees
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.EmployeeId != model.EmployeeId &&
                        x.Email != null &&
                        x.Email == model.Email);

                if (emailExists)
                {
                    ModelState.AddModelError(
                        nameof(model.Email),
                        "Bu e-posta adresi başka bir personelde kullanılıyor.");
                }
            }

            // Admin hesabına bağlı personelin pasifleştirilmesini
            // veya işten ayrılmış olarak işaretlenmesini engeller
            if (employee.User?.Role == UserRole.Admin &&
                (!model.IsActive || model.TerminationDate.HasValue))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Admin hesabına bağlı personel pasifleştirilemez veya işten ayrılmış olarak işaretlenemez.");
            }

            // Yeni profil fotoğrafı seçilmişse doğrular
            if (model.ImageFile != null)
            {
                ValidateImage(
                    model.ImageFile,
                    nameof(model.ImageFile));
            }

            // Model doğrulama başarısızsa formu tekrar gösterir
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? oldImageUrl = employee.ImageUrl;
            string? newImageUrl = null;

            // Yeni fotoğraf seçilmişse önce yeni fotoğrafı kaydeder
            if (model.ImageFile != null)
            {
                newImageUrl =
                    await SaveImageAsync(model.ImageFile);
            }

            // Personel bilgilerini günceller
            employee.FullName = model.FullName;
            employee.Phone = model.Phone;
            employee.Email = model.Email;
            employee.Position = model.Position;
            employee.HireDate = model.HireDate.Date;
            employee.TerminationDate =
                model.TerminationDate?.Date;
            employee.Note = model.Note;
            employee.IsActive = model.IsActive;

            // Yeni fotoğraf yüklendiyse personelin fotoğraf yolunu değiştirir
            if (newImageUrl != null)
            {
                employee.ImageUrl = newImageUrl;
            }

            // İşten ayrılma tarihi girilmişse personeli otomatik pasif yapar
            if (employee.TerminationDate.HasValue)
            {
                employee.IsActive = false;
            }

            try
            {
                await _context.SaveChangesAsync();

                // Personel güncelleme işlemini işlem geçmişine kaydeder
                await _activityLogService.LogAsync(
                    "Employee",
                    "Update",
                    employee.EmployeeId,
                    $"{employee.FullName} adlı personelin bilgileri güncellendi.");
            }
            catch
            {
                // Veritabanı güncellemesi başarısızsa yeni yüklenen fotoğrafı temizler
                if (newImageUrl != null)
                {
                    DeleteImage(newImageUrl);
                }

                throw;
            }

            // Yeni fotoğraf başarıyla kaydedildiyse eski fotoğrafı sunucudan siler
            if (newImageUrl != null &&
                !string.IsNullOrWhiteSpace(oldImageUrl))
            {
                DeleteImage(oldImageUrl);
            }

            TempData["SuccessMessage"] =
                "Personel bilgileri başarıyla güncellendi.";

            return RedirectToAction(
                nameof(Details),
                new { id = employee.EmployeeId });
        }

        // Aktif personeli pasif, pasif personeli aktif hale getirir
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            // Personeli ve varsa kullanıcı hesabını getirir
            Employee? employee = await _context.Employees
                .Include(x => x.User)
                .FirstOrDefaultAsync(
                    x => x.EmployeeId == id);

            if (employee == null)
            {
                return NotFound();
            }

            // İşlem öncesindeki personel durumunu saklar
            bool wasActive = employee.IsActive;

            // Admin hesabına bağlı personelin pasifleştirilmesini engeller
            if (employee.User?.Role == UserRole.Admin && employee.IsActive)
            {
                TempData["ErrorMessage"] =
                    "Admin hesabına bağlı personel pasifleştirilemez.";

                return RedirectToAction(nameof(Index));
            }

            // Aktif personel pasife alınacaksa
            if (employee.IsActive)
            {
                employee.IsActive = false;

                // Personelin sistem hesabı varsa girişini de kapatır
                if (employee.User != null)
                {
                    employee.User.IsActive = false;
                }

                TempData["SuccessMessage"] =
                    "Personel pasif duruma alındı.";
            }
            else
            {
                // İşten ayrılma tarihi bulunan personelin doğrudan aktif edilmesini engeller
                if (employee.TerminationDate.HasValue)
                {
                    TempData["ErrorMessage"] =
                        "İşten ayrılma tarihi bulunan personel doğrudan aktif edilemez. Önce personel bilgilerini düzenleyin.";

                    return RedirectToAction(nameof(Index));
                }

                employee.IsActive = true;

                // Kullanıcı hesabını otomatik aktif etmiyoruz
                // Hesap durumu kullanıcı yönetiminden ayrıca kontrol edilecek

                TempData["SuccessMessage"] =
                    "Personel aktif duruma alındı.";
            }

            await _context.SaveChangesAsync();

            // Personelin aktiflik değişikliğini işlem geçmişine kaydeder
            await _activityLogService.LogAsync(
                "Employee",
                "StatusChange",
                employee.EmployeeId,
                wasActive
                    ? $"{employee.FullName} adlı personel pasif duruma getirildi."
                    : $"{employee.FullName} adlı personel aktif duruma getirildi.");

            return RedirectToAction(nameof(Index));
        }

        // Personeli sistemden siler
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            // Silinecek personeli ve varsa kullanıcı hesabını getirir
            Employee? employee = await _context.Employees
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.EmployeeId == id);

            // Personel bulunamadıysa 404 döndürür
            if (employee == null)
            {
                return NotFound();
            }

            // Sistem kullanıcı hesabı bulunan personelin silinmesini engeller
            if (employee.User != null)
            {
                TempData["ErrorMessage"] =
                    "Bu personele bağlı bir kullanıcı hesabı bulunduğu için personel silinemez. " +
                    "Personeli sistemden kaldırmak için pasif duruma getirebilirsiniz.";

                return RedirectToAction(nameof(Index));
            }

            // İşlem geçmişinde kullanılmak üzere personel bilgilerini saklar
            int employeeId = employee.EmployeeId;
            string employeeFullName = employee.FullName;
            string? imageUrl = employee.ImageUrl;

            // Personeli veritabanından siler
            _context.Employees.Remove(employee);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] =
                    "Bu personel başka kayıtlarla ilişkili olduğu için silinemiyor. " +
                    "Personeli pasif duruma getirebilirsiniz.";

                return RedirectToAction(nameof(Index));
            }

            // Personel silme işlemini işlem geçmişine kaydeder
            await _activityLogService.LogAsync(
                "Employee",
                "Delete",
                employeeId,
                $"{employeeFullName} adlı personel silindi.");

            // Personelin profil fotoğrafı varsa sunucudan kaldırır
            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                DeleteImage(imageUrl);
            }

            TempData["SuccessMessage"] =
                "Personel başarıyla silindi.";

            return RedirectToAction(nameof(Index));
        }

        // Personel fotoğrafını yalnızca Admin yetkisiyle görüntüler
        [HttpGet("/Employee/Photo/{fileName}")]
        public async Task<IActionResult> Photo(string fileName)
        {
            // Dosya adının güvenli olup olmadığını kontrol eder
            if (!IsValidPhotoFileName(fileName))
            {
                return NotFound();
            }

            // Fotoğrafın kayıtlı bir personele ait olduğunu doğrular
            string imageUrl = "/Employee/Photo/" + fileName;

            bool photoExists = await _context.Employees
                .AsNoTracking()
                .AnyAsync(x => x.ImageUrl == imageUrl);

            if (!photoExists)
            {
                return NotFound();
            }

            // Fotoğrafın fiziksel konumunu belirler
            string filePath = Path.Combine(
                GetPrivateEmployeeFolder(),
                fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            // Dosya uzantısına göre içerik türünü belirler
            string extension = Path.GetExtension(fileName).ToLowerInvariant();

            string contentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            // Özel fotoğrafların önbellekte tutulmasını engeller
            Response.Headers.CacheControl = "private, no-store";

            return PhysicalFile(filePath, contentType);
        }

        // Yüklenen profil fotoğrafının geçerli olup olmadığını kontrol eder
        private void ValidateImage(
            IFormFile imageFile,
            string propertyName)
        {
            string? validationError =
                ImageUploadValidator.Validate(
                    imageFile,
                    MaxImageSize);

            if (validationError != null)
            {
                ModelState.AddModelError(
                    propertyName,
                    validationError);
            }
        }
        // Personel fotoğraflarının güvenli klasör yolunu belirler
        private string GetPrivateEmployeeFolder()
        {
            return Path.Combine(
                _environment.ContentRootPath,
                "PrivateUploads",
                "Employees");
        }

        // Fotoğraf dosyasının güvenli bir ada sahip olduğunu doğrular
        private static bool IsValidPhotoFileName(string? fileName)
        {
            // Dosya adı boş olamaz
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            // Dosya adında klasör yolu bulunmasını engeller
            if (Path.GetFileName(fileName) != fileName)
            {
                return false;
            }

            // Dosya uzantısını kontrol eder
            string extension = Path.GetExtension(fileName)
                .ToLowerInvariant();

            if (extension != ".jpg" &&
                extension != ".jpeg" &&
                extension != ".png" &&
                extension != ".webp")
            {
                return false;
            }

            // Dosya adının GUID formatında olup olmadığını kontrol eder
            string name = Path.GetFileNameWithoutExtension(fileName);

            return name.Length == 32 &&
                   Guid.TryParseExact(name, "N", out _);
        }

        // Personel profil fotoğrafını wwwroot dışındaki güvenli klasöre kaydeder
        private async Task<string> SaveImageAsync(
            IFormFile imageFile)
        {
            // Personel fotoğraflarının güvenli klasör yolunu alır
            string uploadFolder = GetPrivateEmployeeFolder();

            // Klasör yoksa oluşturur
            Directory.CreateDirectory(uploadFolder);

            // Dosyanın uzantısını alır
            string extension =
                Path.GetExtension(imageFile.FileName)
                    .ToLowerInvariant();

            // Benzersiz ve tahmin edilmesi zor dosya adı oluşturur
            string fileName =
                $"{Guid.NewGuid():N}{extension}";

            // Fotoğrafın kaydedileceği fiziksel yolu oluşturur
            string filePath = Path.Combine(
                uploadFolder,
                fileName);

            // Fotoğrafı güvenli klasöre kaydeder
            await using FileStream stream = new(
                filePath,
                FileMode.CreateNew);

            await imageFile.CopyToAsync(stream);

            // Veritabanına Admin yetkisi gerektiren fotoğraf adresini kaydeder
            return $"/Employee/Photo/{fileName}";
        }

        // Personel profil fotoğrafını sunucudan güvenli şekilde siler
        private void DeleteImage(string? imageUrl)
        {
            // Fotoğraf yolu boşsa işlem yapmaz
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return;
            }

            const string privatePrefix = "/Employee/Photo/";
            const string legacyPrefix = "/uploads/employees/";

            string fileName;
            string folder;

            // Yeni özel klasördeki fotoğrafları kontrol eder
            if (imageUrl.StartsWith(
                privatePrefix,
                StringComparison.OrdinalIgnoreCase))
            {
                fileName = imageUrl[privatePrefix.Length..];
                folder = GetPrivateEmployeeFolder();
            }
            // Eski fotoğraf klasöründeki dosyaları kontrol eder
            else if (imageUrl.StartsWith(
                legacyPrefix,
                StringComparison.OrdinalIgnoreCase))
            {
                fileName = imageUrl[legacyPrefix.Length..];

                folder = Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "employees");
            }
            else
            {
                return;
            }

            // Güvenli dosya adı kontrolü yapar
            if (!IsValidPhotoFileName(fileName))
            {
                return;
            }

            // Silinecek dosyanın fiziksel yolunu oluşturur
            string filePath = Path.Combine(folder, fileName);

            // Dosya mevcutsa siler
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
        // Boş veya sadece boşluk içeren metinleri null değerine dönüştürür
        private static string? NormalizeNullable(
            string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}