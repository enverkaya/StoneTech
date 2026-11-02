
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

// QuestPDF Community lisans ayarı
QuestPDF.Settings.License =
    QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Ekrandaki tarih ve para gösterimlerini Türkçe biçimde tutar
var turkishCulture = CultureInfo.GetCultureInfo("tr-TR");

CultureInfo.DefaultThreadCurrentCulture = turkishCulture;
CultureInfo.DefaultThreadCurrentUICulture = turkishCulture;

// HTML number alanlarından gelen noktalı decimal değerleri kültürden bağımsız bağlar
builder.Services.AddControllersWithViews(options =>
{
    options.ModelBinderProviders.Insert(
        0,
        new InvariantDecimalModelBinderProvider());
});

// AppDbContext'i uygulamaya tanıtır
// Bağlantı bilgisini appsettings.json içindeki DefaultConnection'dan alır
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Kullanıcı şifrelerini güvenli şekilde hashlemek ve doğrulamak için kullanılır
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// HTTP isteğine ve giriş yapan kullanıcının bilgilerine servislerden erişilmesini sağlar
builder.Services.AddHttpContextAccessor();

// Sistem içerisinde yapılan işlemleri ActivityLogs tablosuna kaydeden servisi tanıtır
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();

// Raporların Excel dosyası olarak oluşturulmasını sağlayan servisi tanıtır
builder.Services.AddScoped<IExcelService, ExcelService>();

// Raporların PDF dosyası olarak oluşturulmasını sağlayan servisi tanıtır
builder.Services.AddScoped<IPdfService, PdfService>();

// Rapor sorgularını ve hesaplamalarını controller katmanından ayırır
builder.Services.AddScoped<IReportService, ReportService>();

// Profesyonel fatura PDF belgelerini oluşturan servisi tanıtır
builder.Services.AddScoped<IInvoicePdfService, InvoicePdfService>();

// Müşterilere e-posta ile cevap gönderen servisi tanıtır
builder.Services.AddScoped<IMailService, MailService>();

// Cookie tabanlı kullanıcı giriş sistemini uygulamaya tanıtır
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Giriş yapmamış kullanıcıların yönlendirileceği sayfa
        options.LoginPath = "/Account/Login";

        // Yetkisi olmayan kullanıcıların yönlendirileceği sayfa
        options.AccessDeniedPath = "/Account/AccessDenied";

        // Kullanıcının giriş oturumunun geçerlilik süresi
        options.ExpireTimeSpan = TimeSpan.FromHours(8);

        // Kullanıcı sistemi aktif kullandıkça oturum süresini yeniler
        options.SlidingExpiration = true;

        // StoneTech giriş cookie'sinin adı
        options.Cookie.Name = "StoneTech.Auth";

        // JavaScript üzerinden authentication cookie'sine erişilmesini engeller
        options.Cookie.HttpOnly = true;

        // Cookie'nin aynı site politikası
        options.Cookie.SameSite = SameSiteMode.Lax;

        // HTTPS kullanıldığında cookie'nin güvenli bağlantı üzerinden gönderilmesini sağlar
        options.Cookie.SecurePolicy =
            builder.Environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;

        // Pasife alınan, işten ayrılan veya rolü değiştirilen kullanıcıların
        // daha önce oluşturulmuş cookie ile eski yetkilerini kullanmasını engeller.
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

// Varsayılan olarak bütün MVC sayfalarının giriş yapmış kullanıcı gerektirmesini sağlar
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Tek bir istemciden kısa sürede sınırsız istek yapılmasını engeller
builder.Services.AddRateLimiter(options =>
{
    // Limit aşıldığında HTTP 429 döndür
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    // Giriş sayfası için IP bazlı hız sınırlaması
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey:
                httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                // Bir dakikada en fazla 10 giriş isteği
                PermitLimit = 10,

                // Bir dakikalık zaman aralığı
                Window = TimeSpan.FromMinutes(1),

                // Bekleme kuyruğu oluşturma
                QueueLimit = 0,

                // Süre dolunca istek hakkını otomatik yenile
                AutoReplenishment = true
            }));

    // İletişim formu için IP bazlı hız sınırlaması
    options.AddPolicy("contact", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey:
                httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                // On dakikada en fazla 5 mesaj gönderme isteği
                PermitLimit = 5,

                // On dakikalık zaman aralığı
                Window = TimeSpan.FromMinutes(10),

                // Bekleme kuyruğu oluşturma
                QueueLimit = 0,

                // Süre dolunca istek hakkını otomatik yenile
                AutoReplenishment = true
            }));
});

var app = builder.Build();

// Production ortamında hata yönetimini ayarlar
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}

app.UseHttpsRedirection();

// Tarayıcının yüklenen dosyaları farklı bir içerik türü gibi yorumlamasını
// ve sayfaların başka siteler içinde çerçevelenmesini sınırlar.
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "SAMEORIGIN";
    context.Response.Headers["Referrer-Policy"] =
        "strict-origin-when-cross-origin";

    await next();
});

app.UseRouting();

// İstek hız sınırlarını uygular
app.UseRateLimiter();

// Cookie üzerinden giriş yapan kullanıcıyı tanır
app.UseAuthentication();

// Personel fotoğrafları kişisel veri içerdiği için doğrudan anonim
// statik dosya isteğiyle indirilmesini engeller.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments(
            "/images/employees",
            StringComparison.OrdinalIgnoreCase) &&
        context.User.Identity?.IsAuthenticated != true)
    {
        context.Response.StatusCode =
            StatusCodes.Status404NotFound;

        return;
    }

    await next();
});

app.UseStaticFiles();

// Kullanıcının rol ve yetkilerini kontrol eder
app.UseAuthorization();

// Varsayılan MVC route ayarı
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
