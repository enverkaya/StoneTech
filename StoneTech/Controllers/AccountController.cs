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
                .FirstOrDefaultAsync(x =>
                    x.Username == model.Username);

            // Kullanıcı bulunamadıysa genel bir hata mesajı gösterir
            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Kullanıcı adı veya şifre hatalı.");

                return View(model);
            }

            // Kullanıcı hesabı pasifse sisteme giriş yapılmasını engeller
            if (!user.IsActive)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Kullanıcı adı veya şifre hatalı.");

                return View(model);
            }

            // Bağlı personel pasif veya işten ayrılmışsa giriş yapılmasını engeller
            if (user.Employee == null ||
                !user.Employee.IsActive ||
                user.Employee.TerminationDate.HasValue)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Kullanıcı adı veya şifre hatalı.");

                return View(model);
            }

            // Girilen şifreyi veritabanındaki hashlenmiş şifreyle karşılaştırır
            var passwordResult =
                _passwordHasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    model.Password);

            // Şifre doğrulanamadıysa giriş işlemini durdurur
            if (passwordResult ==
                PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Kullanıcı adı veya şifre hatalı.");

                return View(model);
            }

            // Hash algoritması güncellenmişse mevcut şifreyi
            // yeni algoritmayla tekrar hashler
            if (passwordResult ==
                PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash =
                    _passwordHasher.HashPassword(
                        user,
                        model.Password);
            }

            // Kullanıcının son başarılı giriş tarihini günceller
            user.LastLoginDate = DateTime.Now;

            await _context.SaveChangesAsync();

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

                // Parola değiştiğinde daha önce verilmiş cookie'leri geçersiz kılar
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
            var authenticationProperties =
                new AuthenticationProperties
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
            // Login isteği AllowAnonymous olduğu için kullanıcı bilgileri
            // doğrudan doğrulanan kullanıcı hesabından alınır
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
