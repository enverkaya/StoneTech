using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoneTech.Data;
using StoneTech.Entities;
using StoneTech.Enums;
using StoneTech.Models.User;
using StoneTech.Services;

namespace StoneTech.Controllers
{
    // Kullanıcı hesaplarının yönetimine yalnızca Admin rolündeki kullanıcıların erişmesini sağlar
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IActivityLogService _activityLogService;

        // Sayfalama için kullanılabilecek kayıt sayıları
        private static readonly int[] AllowedPageSizes = { 5, 10, 20, 50 };

        public UserController(
            AppDbContext context,
            IPasswordHasher<User> passwordHasher,
            IActivityLogService activityLogService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _activityLogService = activityLogService;
        }


        // Kullanıcı hesaplarını listeler
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

            // Geçersiz kayıt sayısını varsayılan değere çevirir
            if (!AllowedPageSizes.Contains(pageSize))
            {
                pageSize = 10;
            }

            // Arama metninin başındaki ve sonundaki boşlukları temizler
            search = string.IsNullOrWhiteSpace(search)
                ? null
                : search.Trim();

            // Kullanıcıları bağlı oldukları personel bilgileriyle birlikte hazırlar
            var query = _context.Users
                .AsNoTracking()
                .Include(x => x.Employee)
                .AsQueryable();

            // Kullanıcı adı, personel adı, görev, telefon veya e-posta üzerinden arama yapar
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.Username.Contains(search) ||
                    x.Employee.FullName.Contains(search) ||
                    x.Employee.Position.Contains(search) ||
                    (x.Employee.Phone != null &&
                     x.Employee.Phone.Contains(search)) ||
                    (x.Employee.Email != null &&
                     x.Employee.Email.Contains(search)));
            }

            // Filtre sonucundaki toplam kayıt sayısını hesaplar
            var totalCount = await query.CountAsync();

            // Toplam sayfa sayısını hesaplar
            var totalPages = totalCount == 0
                ? 1
                : (int)Math.Ceiling(totalCount / (double)pageSize);

            // İstenen sayfa toplam sayfa sayısından büyükse son sayfaya gider
            if (page > totalPages)
            {
                page = totalPages;
            }

            // Kullanıcı listesini ViewModel'e dönüştürür
            var users = await query
                .OrderBy(x => x.Employee.FullName)
                .ThenBy(x => x.Username)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new UserListViewModel
                {
                    UserId = x.UserId,
                    EmployeeId = x.EmployeeId,
                    FullName = x.Employee.FullName,
                    Position = x.Employee.Position,
                    ImageUrl = x.Employee.ImageUrl,
                    Username = x.Username,
                    Role = x.Role,
                    IsActive = x.IsActive,
                    IsEmployeeActive = x.Employee.IsActive,
                    LastLoginDate = x.LastLoginDate
                })
                .ToListAsync();

            // Sayfalama ve arama bilgilerini View'a gönderir
            ViewBag.Search = search;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View(users);
        }

        // Yeni kullanıcı hesabı oluşturma sayfasını açar
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadAvailableEmployeesAsync();

            return View(new UserCreateViewModel
            {
                IsActive = true
            });
        }

        // Yeni kullanıcı hesabı oluşturur
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            // Kullanıcı adındaki gereksiz boşlukları temizler
            model.Username = model.Username?.Trim() ?? string.Empty;

            // Seçilen personeli veritabanından getirir
            var employee = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.EmployeeId == model.EmployeeId);

            // Personel bulunamadıysa işlemi engeller
            if (employee == null)
            {
                ModelState.AddModelError(
                    nameof(model.EmployeeId),
                    "Seçilen personel bulunamadı.");
            }
            else
            {
                // Pasif personele sistem hesabı açılmasını engeller
                if (!employee.IsActive)
                {
                    ModelState.AddModelError(
                        nameof(model.EmployeeId),
                        "Pasif personele sistem kullanıcı hesabı oluşturulamaz.");
                }

                // İşten ayrılmış personele sistem hesabı açılmasını engeller
                if (employee.TerminationDate.HasValue)
                {
                    ModelState.AddModelError(
                        nameof(model.EmployeeId),
                        "İşten ayrılmış personele sistem kullanıcı hesabı oluşturulamaz.");
                }
            }

            // Aynı personele ikinci kullanıcı hesabı açılmasını engeller
            var employeeHasUser = await _context.Users
                .AnyAsync(x => x.EmployeeId == model.EmployeeId);

            if (employeeHasUser)
            {
                ModelState.AddModelError(
                    nameof(model.EmployeeId),
                    "Bu personele ait bir sistem kullanıcı hesabı zaten bulunuyor.");
            }

            // Aynı kullanıcı adının tekrar kullanılmasını engeller
            var usernameExists = await _context.Users
                .AnyAsync(x => x.Username == model.Username);

            if (usernameExists)
            {
                ModelState.AddModelError(
                    nameof(model.Username),
                    "Bu kullanıcı adı zaten kullanılıyor.");
            }

            // Rol değerinin geçerli bir UserRole değeri olup olmadığını kontrol eder
            if (!Enum.IsDefined(model.Role))
            {
                ModelState.AddModelError(
                    nameof(model.Role),
                    "Geçerli bir kullanıcı rolü seçilmelidir.");
            }

            // Validasyon hatası varsa formu tekrar gösterir
            if (!ModelState.IsValid)
            {
                await LoadAvailableEmployeesAsync(model.EmployeeId);
                return View(model);
            }

            // Yeni kullanıcı Entity'sini oluşturur
            var user = new User
            {
                EmployeeId = model.EmployeeId,
                Username = model.Username,
                Role = model.Role,
                IsActive = model.IsActive
            };

            // Şifreyi güvenli şekilde hashleyerek saklanacak değeri oluşturur
            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                model.Password);

            // Kullanıcıyı veritabanına ekler
            _context.Users.Add(user);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Veritabanı seviyesindeki olası benzersizlik çakışmalarını yakalar
                ModelState.AddModelError(
                    string.Empty,
                    "Kullanıcı hesabı oluşturulamadı. Kullanıcı adı veya personel hesabı başka bir kayıt tarafından kullanılmış olabilir.");

                await LoadAvailableEmployeesAsync(model.EmployeeId);
                return View(model);
            }

            await _activityLogService.LogAsync(
                "User",
                "Create",
                user.UserId,
                $"{user.Username} kullanıcı hesabı {user.Role} rolüyle oluşturuldu.");

            TempData["SuccessMessage"] =
                "Kullanıcı hesabı başarıyla oluşturuldu.";

            return RedirectToAction(nameof(Index));
        }

        // Kullanıcı hesabının detaylarını gösterir
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            // Kullanıcıyı bağlı olduğu personel bilgileriyle birlikte getirir
            var user = await _context.Users
                .AsNoTracking()
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.UserId == id);

            // Kullanıcı bulunamadıysa 404 döndürür
            if (user == null)
            {
                return NotFound();
            }

            // Entity bilgisini detay ViewModel'ine dönüştürür
            var model = new UserDetailsViewModel
            {
                UserId = user.UserId,
                EmployeeId = user.EmployeeId,
                FullName = user.Employee.FullName,
                Position = user.Employee.Position,
                Phone = user.Employee.Phone,
                Email = user.Employee.Email,
                ImageUrl = user.Employee.ImageUrl,
                IsEmployeeActive = user.Employee.IsActive,
                Username = user.Username,
                Role = user.Role,
                IsActive = user.IsActive,
                LastLoginDate = user.LastLoginDate
            };

            return View(model);
        }

        // Kullanıcı hesabı düzenleme sayfasını açar
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            // Kullanıcıyı bağlı olduğu personelle birlikte getirir
            var user = await _context.Users
                .AsNoTracking()
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.UserId == id);

            // Kullanıcı bulunamadıysa 404 döndürür
            if (user == null)
            {
                return NotFound();
            }

            // Entity bilgisini düzenleme ViewModel'ine dönüştürür
            var model = new UserEditViewModel
            {
                UserId = user.UserId,
                EmployeeId = user.EmployeeId,
                FullName = user.Employee.FullName,
                Username = user.Username,
                Role = user.Role,
                IsActive = user.IsActive
            };

            return View(model);
        }

        // Kullanıcı hesabındaki değişiklikleri kaydeder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            UserEditViewModel model)
        {
            // Route Id ile form Id bilgisinin aynı olduğunu kontrol eder
            if (id != model.UserId)
            {
                return BadRequest();
            }

            // Kullanıcı adındaki gereksiz boşlukları temizler
            model.Username = model.Username?.Trim() ?? string.Empty;

            // Kullanıcı hesabını personel bilgileriyle birlikte getirir
            var user = await _context.Users
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.UserId == id);

            // Kullanıcı bulunamadıysa 404 döndürür
            if (user == null)
            {
                return NotFound();
            }

            // Formdan gelen personel bilgilerine güvenmek yerine veritabanındaki değerleri kullanır
            model.EmployeeId = user.EmployeeId;
            model.FullName = user.Employee.FullName;

            // Aynı kullanıcı adının başka bir hesap tarafından kullanılmasını engeller
            var usernameExists = await _context.Users
                .AnyAsync(x =>
                    x.UserId != user.UserId &&
                    x.Username == model.Username);

            if (usernameExists)
            {
                ModelState.AddModelError(
                    nameof(model.Username),
                    "Bu kullanıcı adı başka bir kullanıcı hesabı tarafından kullanılıyor.");
            }

            // Rol değerinin geçerli bir UserRole değeri olup olmadığını kontrol eder
            if (!Enum.IsDefined(model.Role))
            {
                ModelState.AddModelError(
                    nameof(model.Role),
                    "Geçerli bir kullanıcı rolü seçilmelidir.");
            }

            // Pasif personelin kullanıcı hesabının aktif edilmesini engeller
            if (model.IsActive &&
                (!user.Employee.IsActive ||
                 user.Employee.TerminationDate.HasValue))
            {
                ModelState.AddModelError(
                    nameof(model.IsActive),
                    "Pasif veya işten ayrılmış personelin kullanıcı hesabı aktif edilemez.");
            }

            // Sistemde en az bir kullanılabilir Admin hesabı kalmasını garanti eder
            if (user.Role == UserRole.Admin &&
                user.IsActive &&
                (model.Role != UserRole.Admin || !model.IsActive))
            {
                bool anotherActiveAdminExists = await _context.Users
                    .AnyAsync(x =>
                        x.UserId != user.UserId &&
                        x.Role == UserRole.Admin &&
                        x.IsActive &&
                        x.Employee.IsActive &&
                        !x.Employee.TerminationDate.HasValue);

                if (!anotherActiveAdminExists)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Sistemde en az bir aktif Admin hesabı bulunmalıdır.");
                }
            }

            // Validasyon hatası varsa formu tekrar gösterir
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Düzenlenmesine izin verilen alanları günceller
            user.Username = model.Username;
            user.Role = model.Role;
            user.IsActive = model.IsActive;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Kullanıcı hesabı güncellenemedi. Kullanıcı adı başka bir kayıt tarafından kullanılıyor olabilir.");

                return View(model);
            }

            await _activityLogService.LogAsync(
                "User",
                "Update",
                user.UserId,
                $"{user.Username} kullanıcı hesabı güncellendi. Rol: {user.Role}, Aktif: {user.IsActive}.");

            TempData["SuccessMessage"] =
                "Kullanıcı hesabı başarıyla güncellendi.";

            return RedirectToAction(
                nameof(Details),
                new { id = user.UserId });
        }

        // Admin tarafından kullanıcı şifresini sıfırlama sayfasını açar
        [HttpGet]
        public async Task<IActionResult> ResetPassword(int id)
        {
            // Kullanıcıyı bağlı olduğu personel bilgileriyle birlikte getirir
            var user = await _context.Users
                .AsNoTracking()
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.UserId == id);

            // Kullanıcı bulunamadıysa 404 döndürür
            if (user == null)
            {
                return NotFound();
            }

            // Şifre sıfırlama ekranında gösterilecek bilgileri hazırlar
            var model = new UserResetPasswordViewModel
            {
                UserId = user.UserId,
                FullName = user.Employee.FullName,
                Username = user.Username
            };

            return View(model);
        }

        // Admin tarafından kullanıcının şifresini sıfırlar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            int id,
            UserResetPasswordViewModel model)
        {
            // Route Id ile formdan gelen kullanıcı Id bilgisini karşılaştırır
            if (id != model.UserId)
            {
                return BadRequest();
            }

            // Kullanıcıyı bağlı olduğu personel bilgileriyle birlikte getirir
            var user = await _context.Users
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.UserId == id);

            // Kullanıcı bulunamadıysa 404 döndürür
            if (user == null)
            {
                return NotFound();
            }

            // Formdan gelen kullanıcı bilgilerine güvenmek yerine veritabanındaki değerleri kullanır
            model.FullName = user.Employee.FullName;
            model.Username = user.Username;

            // Validasyon hatası varsa şifre sıfırlama ekranını tekrar gösterir
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Yeni şifreyi güvenli şekilde hashleyerek eski şifrenin yerine kaydeder
            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                model.NewPassword);

            await _context.SaveChangesAsync();

            await _activityLogService.LogAsync(
                "User",
                "PasswordReset",
                user.UserId,
                $"{user.Username} kullanıcı hesabının şifresi yönetici tarafından sıfırlandı.");

            TempData["SuccessMessage"] =
                $"{user.Employee.FullName} kullanıcısının şifresi başarıyla sıfırlandı.";

            return RedirectToAction(
                nameof(Details),
                new { id = user.UserId });
        }

        // Kullanıcı hesabını aktif veya pasif duruma getirir
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            // Kullanıcıyı bağlı olduğu personelle birlikte getirir
            var user = await _context.Users
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.UserId == id);

            // Kullanıcı bulunamadıysa 404 döndürür
            if (user == null)
            {
                return NotFound();
            }

            // Pasif hesabı tekrar aktif ederken personelin durumunu kontrol eder
            if (!user.IsActive)
            {
                if (!user.Employee.IsActive ||
                    user.Employee.TerminationDate.HasValue)
                {
                    TempData["ErrorMessage"] =
                        "Pasif veya işten ayrılmış personelin kullanıcı hesabı aktif edilemez.";

                    return RedirectToAction(nameof(Index));
                }
            }

            // Son kullanılabilir Admin hesabının pasife alınmasını engeller
            if (user.IsActive && user.Role == UserRole.Admin)
            {
                bool anotherActiveAdminExists = await _context.Users
                    .AnyAsync(x =>
                        x.UserId != user.UserId &&
                        x.Role == UserRole.Admin &&
                        x.IsActive &&
                        x.Employee.IsActive &&
                        !x.Employee.TerminationDate.HasValue);

                if (!anotherActiveAdminExists)
                {
                    TempData["ErrorMessage"] =
                        "Sistemde en az bir aktif Admin hesabı bulunmalıdır.";

                    return RedirectToAction(nameof(Index));
                }
            }

            // Kullanıcı hesabının aktiflik durumunu tersine çevirir
            user.IsActive = !user.IsActive;

            await _context.SaveChangesAsync();

            await _activityLogService.LogAsync(
                "User",
                "StatusChange",
                user.UserId,
                $"{user.Username} kullanıcı hesabı {(user.IsActive ? "aktif" : "pasif")} duruma getirildi.");

            TempData["SuccessMessage"] = user.IsActive
                ? "Kullanıcı hesabı aktif edildi."
                : "Kullanıcı hesabı pasif edildi.";

            return RedirectToAction(nameof(Index));
        }

        // Kullanıcı hesabını sistemden siler
        [HttpPost]
        [ValidateAntiForgeryToken]       
        public async Task<IActionResult> Delete(int id)
        {
            // Giriş yapan kullanıcının Id bilgisini alır
            string? currentUserIdValue =
                User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            int.TryParse(currentUserIdValue, out int currentUserId);

            // Kullanıcının kendi hesabını silmesini engeller
            if (currentUserId == id)
            {
                TempData["ErrorMessage"] =
                    "Kendi kullanıcı hesabınızı silemezsiniz.";

                return RedirectToAction(nameof(Index));
            }

            // Silinecek kullanıcı hesabını bağlı olduğu personelle birlikte getirir
            var user = await _context.Users
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.UserId == id);

            // Kullanıcı bulunamadıysa 404 döndürür
            if (user == null)
            {
                return NotFound();
            }

            // Son kullanılabilir Admin hesabının silinmesini engeller
            if (user.Role == UserRole.Admin &&
                user.IsActive &&
                user.Employee.IsActive &&
                !user.Employee.TerminationDate.HasValue)
            {
                bool anotherActiveAdminExists = await _context.Users
                    .AnyAsync(x =>
                        x.UserId != user.UserId &&
                        x.Role == UserRole.Admin &&
                        x.IsActive &&
                        x.Employee.IsActive &&
                        !x.Employee.TerminationDate.HasValue);

                if (!anotherActiveAdminExists)
                {
                    TempData["ErrorMessage"] =
                        "Sistemde en az bir aktif Admin hesabı bulunmalıdır.";

                    return RedirectToAction(nameof(Index));
                }
            }

            string deletedUsername = user.Username;

            // Yalnızca kullanıcı hesabını siler
            // Bağlı personel kaydı sistemde kalmaya devam eder
            _context.Users.Remove(user);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] =
                    "Kullanıcı hesabı silinemedi.";

                return RedirectToAction(nameof(Index));
            }

            // Kullanıcı silme işlemini işlem geçmişine kaydeder
            await _activityLogService.LogAsync(
                "User",
                "Delete",
                id,
                $"{deletedUsername} kullanıcı hesabı silindi.");

            TempData["SuccessMessage"] =
                "Kullanıcı hesabı başarıyla silindi. Personel kaydı korunmuştur.";

            return RedirectToAction(nameof(Index));
        }

        // Kullanıcı hesabı olmayan aktif personelleri Create ekranına hazırlar
        private async Task LoadAvailableEmployeesAsync(
            int? selectedEmployeeId = null)
        {
            // Kullanıcı hesabı olmayan aktif personelleri getirir
            var employees = await _context.Employees
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    !x.TerminationDate.HasValue &&
                    !_context.Users.Any(u =>
                        u.EmployeeId == x.EmployeeId))
                .OrderBy(x => x.FullName)
                .Select(x => new
                {
                    x.EmployeeId,
                    x.FullName,
                    x.Position
                })
                .ToListAsync();

            // Validasyon hatasında seçilmiş personelin listeden kaybolmasını önler
            if (selectedEmployeeId.HasValue &&
                !employees.Any(x =>
                    x.EmployeeId == selectedEmployeeId.Value))
            {
                var selectedEmployee = await _context.Employees
                    .AsNoTracking()
                    .Where(x =>
                        x.EmployeeId == selectedEmployeeId.Value)
                    .Select(x => new
                    {
                        x.EmployeeId,
                        x.FullName,
                        x.Position
                    })
                    .FirstOrDefaultAsync();

                if (selectedEmployee != null)
                {
                    employees.Add(selectedEmployee);
                }
            }

            // Select listesinde gösterilecek personel verilerini View'a gönderir
            ViewBag.Employees = employees
                .OrderBy(x => x.FullName)
                .ToList();
        }

        // Silinen personele ait profil fotoğrafını sunucudan kaldırır
        private void DeleteEmployeeImage(string? imageUrl)
        {
            // Fotoğraf yolu boşsa işlem yapmaz
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return;
            }

            // Yalnızca personel yükleme klasöründeki dosyaların silinmesine izin verir
            const string employeeUploadPrefix = "/uploads/employees/";

            if (!imageUrl.StartsWith(
                    employeeUploadPrefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Web yolunu fiziksel dosya yoluna dönüştürür
            string relativePath = imageUrl
                .TrimStart('/')
                .Replace(
                    '/',
                    Path.DirectorySeparatorChar);

            string filePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                relativePath);

            // Dosya mevcutsa siler
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
    }
}
