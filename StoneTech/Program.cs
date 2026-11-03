
using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StoneTech.Data;
using StoneTech.Entities;
using StoneTech.ModelBinding;
using StoneTech.Services;
using StoneTech.Services.Reports;

// QuestPDF Community lisans ayarý
QuestPDF.Settings.License =
    QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Ekrandaki tarih ve para gösterimlerini Türkçe biçimde tutar
var turkishCulture = CultureInfo.GetCultureInfo("tr-TR");

CultureInfo.DefaultThreadCurrentCulture = turkishCulture;
CultureInfo.DefaultThreadCurrentUICulture = turkishCulture;

// HTML number alanlarýndan gelen noktalý decimal deðerleri kültürden baðýmsýz baðlar
builder.Services.AddControllersWithViews(options =>
{
    options.ModelBinderProviders.Insert(
        0,
        new InvariantDecimalModelBinderProvider());
});

// AppDbContext'i uygulamaya tanýtýr
// Baðlantý bilgisini appsettings.json içindeki DefaultConnection'dan alýr
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Kullanýcý þifrelerini güvenli þekilde hashlemek ve doðrulamak için kullanýlýr
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// HTTP isteðine ve giriþ yapan kullanýcýnýn bilgilerine servislerden eriþilmesini saðlar
builder.Services.AddHttpContextAccessor();

// Sistem içerisinde yapýlan iþlemleri ActivityLogs tablosuna kaydeden servisi tanýtýr
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();

// Raporlarýn Excel dosyasý olarak oluþturulmasýný saðlayan servisi tanýtýr
builder.Services.AddScoped<IExcelService, ExcelService>();

// Raporlarýn PDF dosyasý olarak oluþturulmasýný saðlayan servisi tanýtýr
builder.Services.AddScoped<IPdfService, PdfService>();

// Rapor sorgularýný ve hesaplamalarýný controller katmanýndan ayýrýr
builder.Services.AddScoped<IReportService, ReportService>();

// Profesyonel fatura PDF belgelerini oluþturan servisi tanýtýr
builder.Services.AddScoped<IInvoicePdfService, InvoicePdfService>();

// Müþterilere e-posta ile cevap gönderen servisi tanýtýr
builder.Services.AddScoped<IMailService, MailService>();

// Cookie tabanlý kullanýcý giriþ sistemini uygulamaya tanýtýr
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Giriþ yapmamýþ kullanýcýlarýn yönlendirileceði sayfa
        options.LoginPath = "/Account/Login";

        // Yetkisi olmayan kullanýcýlarýn yönlendirileceði sayfa
        options.AccessDeniedPath = "/Account/AccessDenied";

        // Kullanýcýnýn giriþ oturumunun geçerlilik süresi
        options.ExpireTimeSpan = TimeSpan.FromHours(8);

        // Kullanýcý sistemi aktif kullandýkça oturum süresini yeniler
        options.SlidingExpiration = true;

        // StoneTech giriþ cookie'sinin adý
        options.Cookie.Name = "StoneTech.Auth";

        // JavaScript üzerinden authentication cookie'sine eriþilmesini engeller
        options.Cookie.HttpOnly = true;

        // Cookie'nin ayný site politikasý
        options.Cookie.SameSite = SameSiteMode.Lax;

        // HTTPS kullanýldýðýnda cookie'nin güvenli baðlantý üzerinden gönderilmesini saðlar
        options.Cookie.SecurePolicy =
            builder.Environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;

        // Pasife alýnan, iþten ayrýlan veya rolü deðiþtirilen kullanýcýlarýn
        // daha önce oluþturulmuþ cookie ile eski yetkilerini kullanmasýný engeller.
        options.Events.OnValidatePrincipal = async context =>
        {
            string? userIdValue = context.Principal?
                .FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int userId))
            {
                context.RejectPrincipal();

                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);

                return;
            }

            var dbContext = context.HttpContext.RequestServices
                .GetRequiredService<AppDbContext>();

            var currentUser = await dbContext.Users
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => new
                {
                    x.IsActive,
                    x.Role,
                    x.PasswordHash,
                    EmployeeIsActive = x.Employee.IsActive,
                    x.Employee.TerminationDate
                })
                .FirstOrDefaultAsync();

            string? cookieRole = context.Principal?
                .FindFirstValue(ClaimTypes.Role);

            string? cookiePasswordVersion = context.Principal?
                .FindFirstValue(
                    AuthenticationClaimValues.PasswordVersionClaimType);

            bool principalIsValid =
                currentUser != null &&
                currentUser.IsActive &&
                currentUser.EmployeeIsActive &&
                !currentUser.TerminationDate.HasValue &&
                string.Equals(
                    cookieRole,
                    currentUser.Role.ToString(),
                    StringComparison.Ordinal) &&
                string.Equals(
                    cookiePasswordVersion,
                    AuthenticationClaimValues.CreatePasswordVersion(
                        currentUser.PasswordHash),
                    StringComparison.Ordinal);

            if (!principalIsValid)
            {
                context.RejectPrincipal();

                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

// Varsayýlan olarak bütün MVC sayfalarýnýn giriþ yapmýþ kullanýcý gerektirmesini saðlar
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Tek bir istemciden kýsa sürede sýnýrsýz istek yapýlmasýný engeller
builder.Services.AddRateLimiter(options =>
{
    // Limit aþýldýðýnda HTTP 429 döndür
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    // Giriþ sayfasý için IP bazlý hýz sýnýrlamasý
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey:
                httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                // Bir dakikada en fazla 10 giriþ isteði
                PermitLimit = 10,

                // Bir dakikalýk zaman aralýðý
                Window = TimeSpan.FromMinutes(1),

                // Bekleme kuyruðu oluþturma
                QueueLimit = 0,

                // Süre dolunca istek hakkýný otomatik yenile
                AutoReplenishment = true
            }));

    // Ýletiþim formu için IP bazlý hýz sýnýrlamasý
    options.AddPolicy("contact", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey:
                httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                // On dakikada en fazla 5 mesaj gönderme isteði
                PermitLimit = 5,

                // On dakikalýk zaman aralýðý
                Window = TimeSpan.FromMinutes(10),

                // Bekleme kuyruðu oluþturma
                QueueLimit = 0,

                // Süre dolunca istek hakkýný otomatik yenile
                AutoReplenishment = true
            }));
});

var app = builder.Build();

// Production ortamýnda hata yönetimini ayarlar
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}

app.UseHttpsRedirection();

// Tarayýcýnýn yüklenen dosyalarý farklý bir içerik türü gibi yorumlamasýný
// ve sayfalarýn baþka siteler içinde çerçevelenmesini sýnýrlar.
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "SAMEORIGIN";
    context.Response.Headers["Referrer-Policy"] =
        "strict-origin-when-cross-origin";

    await next();
});

app.UseRouting();

// Ýstek hýz sýnýrlarýný uygular
app.UseRateLimiter();

// Cookie üzerinden giriþ yapan kullanýcýyý tanýr
app.UseAuthentication();

// Eski personel fotoðraflarýnýn bulunduðu herkese açýk klasöre
// doðrudan HTTP eriþimini engeller.
// Yeni personel fotoðraflarý EmployeeController.Photo üzerinden
// yalnýzca Admin yetkisiyle görüntülenir.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments(
            "/uploads/employees",
            StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode =
            StatusCodes.Status404NotFound;

        return;
    }

    await next();
});

app.UseStaticFiles();

// Kullanýcýnýn rol ve yetkilerini kontrol eder
app.UseAuthorization();

// Varsayýlan MVC route ayarý
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
