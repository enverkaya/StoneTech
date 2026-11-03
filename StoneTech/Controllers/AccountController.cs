using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StoneTech.Data;
using StoneTech.Entities;
using StoneTech.Models.Account;
using StoneTech.Services;

namespace StoneTech.Controllers
{

    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IActivityLogService _activityLogService;

        // Veritabanı, şifreleme ve işlem geçmişi servisleri alınır
        public AccountController(
            AppDbContext context,
            IPasswordHasher<User> passwordHasher,
            IActivityLogService activityLogService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _activityLogService = activityLogService;
        }

        // Kullanıcı giriş ekranını açar
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Kullanıcı zaten giriş yaptıysa tekrar giriş ekranını göstermez
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            // Yetki gerektiren sayfadan geldiyse dönüş adresini saklar
            ViewData["ReturnUrl"] = returnUrl;

            return View(new LoginViewModel());
        }

        // Kullanıcının giriş bilgilerini kontrol eder ve oturum açar
        [AllowAnonymous]
        [HttpPost]
        [EnableRateLimiting("login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model,
            string? returnUrl = null)
        {
            // Kullanıcı zaten giriş yaptıysa tekrar giriş işlemi yapmaz
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            // Kullanıcı adındaki gereksiz boşlukları temizler
            model.Username = model.Username?.Trim() ?? string.Empty;

            // Dönüş adresini View'a tekrar gönderir
            ViewData["ReturnUrl"] = returnUrl;

            // Form validasyonu başarısızsa giriş ekranını tekrar gösterir
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Kullanıcı hesabını bağlı olduğu personelle birlikte getirir
            var user = await _context.Users
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.Username == model.Username);

            // Güvenlik nedeniyle giriş hatalarında aynı mesajı kullanır
            const string loginError = "Kullanıcı adı veya şifre hatalı.";

            // Kullanıcı bulunamadıysa giriş işlemini durdurur
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, loginError);
                return View(model);
            }

            // Kullanıcı hesabı pasifse giriş işlemini durdurur
            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, loginError);
                return View(model);
            }

            // Bağlı personel aktif değilse giriş işlemini durdurur
            if (user.Employee == null ||
                !user.Employee.IsActive ||
                user.Employee.TerminationDate.HasValue)
            {
                ModelState.AddModelError(string.Empty, loginError);
                return View(model);
            }

            // Geçici kilidin devam edip etmediğini kontrol eder
            var now = DateTime.UtcNow;

            if (user.LockoutEndUtc.HasValue &&
                user.LockoutEndUtc.Value > now)
            {
                // Kilidin bitmesine kalan süreyi dakika olarak hesaplar
                int remainingMinutes = (int)Math.Ceiling(
                    (user.LockoutEndUtc.Value - now).TotalMinutes);

                // Hesap durumunu açıklamadan genel giriş hatasını gösterir
                ModelState.AddModelError(string.Empty, loginError);

                return View(model);
            }
            // Girilen şifreyi hashlenmiş şifreyle karşılaştırır
            var passwordResult = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                model.Password);

            // Şifre hatalıysa başarısız deneme sayısını veritabanında atomik artırır
            if (passwordResult == PasswordVerificationResult.Failed)
            {
                var failedAt = DateTime.UtcNow;
                var lockoutUntil = failedAt.AddMinutes(15);

                await _context.Users
                    .Where(x =>
                        x.UserId == user.UserId &&
                        (x.LockoutEndUtc == null ||
                         x.LockoutEndUtc <= failedAt))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(
                            x => x.FailedLoginCount,
                            x => x.LockoutEndUtc != null &&
                                 x.LockoutEndUtc <= failedAt
                                ? 1
                                : x.FailedLoginCount + 1)
                        .SetProperty(
                            x => x.LockoutEndUtc,
                            x => x.LockoutEndUtc != null &&
                                 x.LockoutEndUtc <= failedAt
                                ? (DateTime?)null
                                : x.FailedLoginCount >= 4
                                    ? (DateTime?)lockoutUntil
                                    : null));

                // Şifre denemesinden sonra hesabın kilitlenip kilitlenmediğini kontrol eder
                var lockoutEnd = await _context.Users
                    .Where(x => x.UserId == user.UserId)
                    .Select(x => x.LockoutEndUtc)
                    .FirstOrDefaultAsync();

                // Hesap kilitlense de kullanıcı varlığı hakkında bilgi vermez
                ModelState.AddModelError(string.Empty, loginError);

                return View(model);
            }

            // Şifre doğru olsa bile eş zamanlı oluşan kilidi kontrol eder
            // ve giriş sayacını sıfırlar
            var loginAcceptedAt = DateTime.UtcNow;

            var updatedRows = await _context.Users
                .Where(x =>
                    x.UserId == user.UserId &&
                    x.IsActive &&
                    x.Employee.IsActive &&
                    x.Employee.TerminationDate == null &&
                    (x.LockoutEndUtc == null ||
                     x.LockoutEndUtc <= loginAcceptedAt))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.FailedLoginCount, 0)
                    .SetProperty(x => x.LockoutEndUtc, (DateTime?)null)
                    .SetProperty(x => x.LastLoginDate, DateTime.Now));

            // Hesap bu sırada kilitlendiyse oturum açılmasını engeller
            if (updatedRows == 0)
            {
                ModelState.AddModelError(string.Empty, loginError);
                return View(model);
            }

            // Hash algoritması güncellenmişse şifreyi yeniden hashler
            if (passwordResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(
                    user,
                    model.Password);

                await _context.Users
                    .Where(x => x.UserId == user.UserId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.PasswordHash, user.PasswordHash));
            }

            // Giriş yapan kullanıcı için kimlik ve yetki bilgilerini hazırlar
            var claims = new List<Claim>
    {
        // Kullanıcı hesabının benzersiz Id bilgisi
        new(
            ClaimTypes.NameIdentifier,
            user.UserId.ToString()),

        // Uygulama içerisinde gösterilecek personel adı
        new(
            ClaimTypes.Name,
            user.Employee.FullName),

        // Kullanıcının sistem rolü
        new(
            ClaimTypes.Role,
            user.Role.ToString()),

        // Kullanıcı hesabının bağlı olduğu personel Id bilgisi
        new(
            "EmployeeId",
            user.EmployeeId.ToString()),

        // Sisteme girişte kullanılan kullanıcı adı
        new(
            "Username",
            user.Username),

        // Parola değiştiğinde eski cookie'leri geçersiz kılar
        new(
            AuthenticationClaimValues.PasswordVersionClaimType,
            AuthenticationClaimValues.CreatePasswordVersion(
                user.PasswordHash))
    };

            // Claims bilgilerini Cookie Authentication kimliğine dönüştürür
            var claimsIdentity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            // Cookie oturumunun kalıcı olup olmayacağını belirler
            var authenticationProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,

                // Kalıcı oturum seçildiyse cookie için bitiş tarihi belirler
                ExpiresUtc = model.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(7)
                    : null,

                // Kullanıcı sistemi kullandıkça oturumun yenilenmesine izin verir
                AllowRefresh = true
            };

            // Kullanıcı için authentication cookie'sini oluşturur
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authenticationProperties);

            // Başarılı giriş işlemini işlem geçmişine kaydeder
            var loginLog = new ActivityLog
            {
                UserId = user.UserId,
                UserFullName = user.Employee.FullName,
                Username = user.Username,
                Module = "Authentication",
                Action = "Login",
                RecordId = user.UserId,
                Description =
                    $"{user.Employee.FullName} sisteme giriş yaptı.",
                CreatedDate = DateTime.Now,
                IpAddress =
                    HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            _context.ActivityLogs.Add(loginLog);

            await _context.SaveChangesAsync();

            // Güvenli bir yerel dönüş adresi varsa kullanıcıyı oraya gönderir
            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            // Normal girişte ana sayfaya yönlendirir
            return RedirectToAction("Index", "Home");
        }

        // Kullanıcının mevcut oturumunu kapatır
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Oturum kapatılmadan önce kullanıcının çıkış işlemini kaydeder
            // Böylece mevcut kullanıcı claim bilgileri henüz erişilebilir durumdadır
            await _activityLogService.LogAsync(
                "Authentication",
                "Logout",
                null,
                $"{User.Identity?.Name ?? "Kullanıcı"} sistemden çıkış yaptı.");

            // Kullanıcının authentication cookie'sini siler
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction(nameof(Login));
        }

        // Kullanıcının erişim yetkisi bulunmayan durumlarda
        // gösterilecek sayfayı açar
        [Authorize]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
