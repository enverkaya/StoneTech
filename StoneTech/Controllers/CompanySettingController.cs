
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using StoneTech.Data;
using StoneTech.Entities;
using StoneTech.Models.CompanySetting;
using StoneTech.Services;

namespace StoneTech.Controllers
{
    // Firma ayarlarına yalnızca Admin rolündeki kullanıcıların erişmesini sağlar
    [Authorize(Roles = "Admin")]
    public class CompanySettingController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IActivityLogService _activityLogService;

        public CompanySettingController(
            AppDbContext context,
            IWebHostEnvironment environment,
            IActivityLogService activityLogService)
        {
            _context = context;
            _environment = environment;
            _activityLogService = activityLogService;
        }

        // Firma ayarları ekranını açar
        // Sistemde firma kaydı varsa mevcut bilgileri getirir
        // Kayıt yoksa boş form gösterir
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var companySetting = await _context.CompanySettings
                .AsNoTracking()
                .OrderBy(x => x.CompanySettingId)
                .FirstOrDefaultAsync();

            // Henüz firma bilgileri oluşturulmamışsa boş form gösterilir
            if (companySetting == null)
            {
                return View(new CompanySettingViewModel());
            }

            // Entity bilgileri ViewModel'e aktarılır
            var model = new CompanySettingViewModel
            {
                CompanySettingId = companySetting.CompanySettingId,
                CompanyName = companySetting.CompanyName,
                TaxOffice = companySetting.TaxOffice,
                TaxNumber = companySetting.TaxNumber,
                Address = companySetting.Address,
                Phone = companySetting.Phone,
                Email = companySetting.Email,
                Website = companySetting.Website,

                // Mevcut firma logoları
                ExistingLogoUrl = companySetting.LogoUrl,
                ExistingSecondLogoUrl = companySetting.SecondLogoUrl,

                // Sosyal medya bağlantıları
                InstagramUrl = companySetting.InstagramUrl,
                FacebookUrl = companySetting.FacebookUrl,
                WhatsAppUrl = companySetting.WhatsAppUrl
            };

            return View(model);
        }

        // Firma bilgilerini kaydeder veya mevcut firma bilgilerini günceller
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CompanySettingViewModel model)
        {
            // Sistemdeki mevcut firma kaydı alınır
            // Firma ayarları tek kayıt mantığında çalışır
            var companySetting = await _context.CompanySettings
                .OrderBy(x => x.CompanySettingId)
                .FirstOrDefaultAsync();

            // Birinci logonun veritabanındaki mevcut adresini saklar
            string? oldLogoUrl = companySetting?.LogoUrl;

            // İkinci logonun veritabanındaki mevcut adresini saklar
            string? oldSecondLogoUrl = companySetting?.SecondLogoUrl;

            // Mevcut logo yollarını form için veritabanından doldurur
            // İstemciden gönderilen gizli alanlara güvenilmez
            model.ExistingLogoUrl = oldLogoUrl;
            model.ExistingSecondLogoUrl = oldSecondLogoUrl;

            if (companySetting != null)
            {
                model.CompanySettingId = companySetting.CompanySettingId;
            }

            // Birinci logo için dosya doğrulaması
            if (model.LogoFile != null)
            {
                const long maxFileSize = 5 * 1024 * 1024;

                var validationError = ImageUploadValidator.Validate(
                    model.LogoFile,
                    maxFileSize);

                if (validationError != null)
                {
                    ModelState.AddModelError(
                        nameof(model.LogoFile),
                        validationError);
                }
            }

            // İkinci logo için dosya doğrulaması
            if (model.SecondLogoFile != null)
            {
                const long maxFileSize = 5 * 1024 * 1024;

                var validationError = ImageUploadValidator.Validate(
                    model.SecondLogoFile,
                    maxFileSize);

                if (validationError != null)
                {
                    ModelState.AddModelError(
                        nameof(model.SecondLogoFile),
                        validationError);
                }
            }

            // Form doğrulama hatası varsa bilgiler kaydedilmez
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Form alanlarındaki gereksiz boşluklar temizlenir
            var companyName = model.CompanyName.Trim();
            var taxOffice = model.TaxOffice.Trim();
            var taxNumber = model.TaxNumber.Trim();
            var address = model.Address.Trim();

            var phone = NormalizeNullable(model.Phone);
            var email = NormalizeNullable(model.Email);
            var website = NormalizeNullable(model.Website);

            // Sosyal medya bağlantılarındaki gereksiz boşlukları temizler
            var instagramUrl = NormalizeNullable(model.InstagramUrl);
            var facebookUrl = NormalizeNullable(model.FacebookUrl);
            var whatsAppUrl = NormalizeNullable(model.WhatsAppUrl);

            string? newLogoUrl = null;
            string? newLogoPhysicalPath = null;

            string? newSecondLogoUrl = null;
            string? newSecondLogoPhysicalPath = null;

            try
            {
                // Birinci logo seçilmişse dosya kaydedilir
                if (model.LogoFile != null)
                {
                    var extension = Path
                        .GetExtension(model.LogoFile.FileName)
                        .ToLowerInvariant();

                    var fileName = $"{Guid.NewGuid():N}{extension}";

                    var logoDirectory = Path.Combine(
                        _environment.WebRootPath,
                        "uploads",
                        "company");

                    Directory.CreateDirectory(logoDirectory);

                    newLogoPhysicalPath = Path.Combine(
                        logoDirectory,
                        fileName);

                    await using (var stream = new FileStream(
                        newLogoPhysicalPath,
                        FileMode.CreateNew))
                    {
                        await model.LogoFile.CopyToAsync(stream);
                    }

                    newLogoUrl = $"/uploads/company/{fileName}";
                }

                // İkinci logo seçilmişse dosya kaydedilir
                if (model.SecondLogoFile != null)
                {
                    var extension = Path
                        .GetExtension(model.SecondLogoFile.FileName)
                        .ToLowerInvariant();

                    var fileName = $"{Guid.NewGuid():N}{extension}";

                    var logoDirectory = Path.Combine(
                        _environment.WebRootPath,
                        "uploads",
                        "company");

                    Directory.CreateDirectory(logoDirectory);

                    newSecondLogoPhysicalPath = Path.Combine(
                        logoDirectory,
                        fileName);

                    await using (var stream = new FileStream(
                        newSecondLogoPhysicalPath,
                        FileMode.CreateNew))
                    {
                        await model.SecondLogoFile.CopyToAsync(stream);
                    }

                    newSecondLogoUrl = $"/uploads/company/{fileName}";
                }

                // Sistemde henüz firma kaydı yoksa ilk kayıt oluşturulur
                if (companySetting == null)
                {
                    companySetting = new CompanySetting
                    {
                        CompanyName = companyName,
                        TaxOffice = taxOffice,
                        TaxNumber = taxNumber,
                        Address = address,
                        Phone = phone,
                        Email = email,
                        Website = website,

                        // Firma logoları
                        LogoUrl = newLogoUrl,
                        SecondLogoUrl = newSecondLogoUrl,

                        // Sosyal medya bağlantıları
                        InstagramUrl = instagramUrl,
                        FacebookUrl = facebookUrl,
                        WhatsAppUrl = whatsAppUrl
                    };

                    _context.CompanySettings.Add(companySetting);
                }
                else
                {
                    // Mevcut firma bilgileri güncellenir
                    companySetting.CompanyName = companyName;
                    companySetting.TaxOffice = taxOffice;
                    companySetting.TaxNumber = taxNumber;
                    companySetting.Address = address;
                    companySetting.Phone = phone;
                    companySetting.Email = email;
                    companySetting.Website = website;

                    // Sosyal medya bağlantıları güncellenir
                    companySetting.InstagramUrl = instagramUrl;
                    companySetting.FacebookUrl = facebookUrl;
                    companySetting.WhatsAppUrl = whatsAppUrl;

                    // Yeni birinci logo seçilmişse güncellenir
                    if (newLogoUrl != null)
                    {
                        companySetting.LogoUrl = newLogoUrl;
                    }

                    // Yeni ikinci logo seçilmişse güncellenir
                    if (newSecondLogoUrl != null)
                    {
                        companySetting.SecondLogoUrl = newSecondLogoUrl;
                    }
                }

                // Firma bilgileri veritabanına kaydedilir
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Hata durumunda yeni yüklenen birinci logo temizlenir
                if (newLogoPhysicalPath != null &&
                    System.IO.File.Exists(newLogoPhysicalPath))
                {
                    System.IO.File.Delete(newLogoPhysicalPath);
                }

                // Hata durumunda yeni yüklenen ikinci logo temizlenir
                if (newSecondLogoPhysicalPath != null &&
                    System.IO.File.Exists(newSecondLogoPhysicalPath))
                {
                    System.IO.File.Delete(newSecondLogoPhysicalPath);
                }

                ModelState.AddModelError(
                    string.Empty,
                    "Firma bilgileri kaydedilirken bir hata oluştu.");

                return View(model);
            }

            // Birinci logo değiştirildiyse eski dosyası silinir
            // Eski adres formdan değil, veritabanından alınmıştır
            if (newLogoUrl != null &&
                !string.IsNullOrWhiteSpace(oldLogoUrl) &&
                !string.Equals(
                    oldLogoUrl,
                    newLogoUrl,
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(
                    oldLogoUrl,
                    companySetting.SecondLogoUrl,
                    StringComparison.OrdinalIgnoreCase))
            {
                DeleteOldLogo(oldLogoUrl);
            }

            // İkinci logo değiştirildiyse eski dosyası silinir
            // Eski adres formdan değil, veritabanından alınmıştır
            if (newSecondLogoUrl != null &&
                !string.IsNullOrWhiteSpace(oldSecondLogoUrl) &&
                !string.Equals(
                    oldSecondLogoUrl,
                    newSecondLogoUrl,
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(
                    oldSecondLogoUrl,
                    companySetting.LogoUrl,
                    StringComparison.OrdinalIgnoreCase))
            {
                DeleteOldLogo(oldSecondLogoUrl);
            }

            // Firma ayarları güncellemesini işlem geçmişine kaydeder
            await _activityLogService.LogAsync(
                "CompanySetting",
                "Update",
                companySetting.CompanySettingId,
                "Firma ayarları güncellendi.");

            TempData["SuccessMessage"] =
                "Firma bilgileri başarıyla kaydedildi.";

            // POST sonrası formun tekrar gönderilmesini engeller
            return RedirectToAction(nameof(Index));
        }

        // Boş veya yalnızca boşluk içeren isteğe bağlı alanları null değerine dönüştürür
        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        // Yeni logo yüklendikten sonra artık kullanılmayan eski logoyu siler
        private void DeleteOldLogo(string logoUrl)
        {
            // Yalnızca firma logo klasöründeki adresleri kabul eder
            const string companyLogoPrefix = "/uploads/company/";

            if (string.IsNullOrWhiteSpace(logoUrl) ||
                !logoUrl.StartsWith(
                    companyLogoPrefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Logo adresinden yalnızca dosya adını alır
            string fileName = logoUrl.Substring(
                companyLogoPrefix.Length);

            // Alt klasör, yol geçişi ve özel yol karakterlerini reddeder
            if (string.IsNullOrWhiteSpace(fileName) ||
                fileName != Path.GetFileName(fileName) ||
                fileName.Contains('/') ||
                fileName.Contains('\\') ||
                fileName.Contains(':'))
            {
                return;
            }

            // Dosyanın uzantısını doğrular
            string extension = Path
                .GetExtension(fileName)
                .ToLowerInvariant();

            if (extension != ".jpg" &&
                extension != ".jpeg" &&
                extension != ".png" &&
                extension != ".webp")
            {
                return;
            }

            // Dosya adının GUID biçiminde oluşturulduğunu doğrular
            string nameWithoutExtension =
                Path.GetFileNameWithoutExtension(fileName);

            if (!Guid.TryParseExact(
                nameWithoutExtension,
                "N",
                out _))
            {
                return;
            }

            // Firma logolarının tutulduğu gerçek klasörü belirler
            string logoDirectory = Path.GetFullPath(
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "company"));

            // Silinecek dosyanın gerçek tam yolunu oluşturur
            string physicalPath = Path.GetFullPath(
                Path.Combine(
                    logoDirectory,
                    fileName));

            // Dosyanın firma logo klasörünün dışına çıkmasını engeller
            string allowedDirectory = Path.EndsInDirectorySeparator(
                logoDirectory)
                ? logoDirectory
                : logoDirectory + Path.DirectorySeparatorChar;

            if (!physicalPath.StartsWith(
                allowedDirectory,
                OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
            {
                return;
            }

            // Yalnızca doğrulanan eski logo dosyasını siler
            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }
        }
    }
}
