
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StoneTech.Data;
using StoneTech.Entities;
using System.ComponentModel.DataAnnotations;

namespace StoneTech.Controllers
{
    [Authorize(Roles = "Admin")]
    [AllowAnonymous]
    public class WebController : Controller
    {
        // Veritabanı bağlantısı
        private readonly AppDbContext _context;

        // Constructor
        public WebController(AppDbContext context)
        {
            _context = context;
        }

        // Ana sayfa
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Aktif slider görsellerini getir
            var sliders = await _context.Set<Slider>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();

            // Logoyu getirir
            var companySetting = await _context.Set<CompanySetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync();

            ViewBag.CompanyLogo = companySetting?.LogoUrl;
            ViewBag.SecondCompanyLogo = companySetting?.SecondLogoUrl;

            // Hakkımızda bilgilerini getir
            var about = await _context.Set<About>()
                .AsNoTracking()
                .FirstOrDefaultAsync();

            // Aktif hizmetleri getir
            var services = await _context.Set<Service>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();

            // Taşlar geliyor
            var stones = await _context.Set<Stone>()
                .AsNoTracking()
                .Where(x => x.IsActive && x.ShowOnWeb)
                .OrderBy(x => x.StoneId)
                .Take(4)
                .ToListAsync();

            ViewBag.Stones = stones;

            // Aktif neden biz özelliklerini getir
            var whyChooseUs = await _context.Set<WhyChooseUs>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();

            // Web sitesinde gösterilecek aktif galeri görselleri
            var galleries = await _context.Set<Gallery>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();

            ViewBag.Galleries = galleries;

            // Ana sayfa verilerini View'a aktar
            ViewBag.Sliders = sliders;
            ViewBag.About = about;
            ViewBag.Services = services;
            ViewBag.Stones = stones;
            ViewBag.WhyChooseUs = whyChooseUs;

            return View();
        }

        // Hakkımızda sayfası
        [HttpGet]
        public async Task<IActionResult> About()
        {
            // Firma logolarını getir
            var companySetting = await _context.Set<CompanySetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync();

            ViewBag.CompanyLogo = companySetting?.LogoUrl;
            ViewBag.SecondCompanyLogo = companySetting?.SecondLogoUrl;

            // Hakkımızda bilgilerini getir
            var about = await _context.Set<About>()
                .AsNoTracking()
                .FirstOrDefaultAsync();

            // Aktif neden biz özelliklerini getir
            var whyChooseUs = await _context.Set<WhyChooseUs>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();

            // Hakkımızda sayfasına verileri aktar
            ViewBag.About = about;
            ViewBag.WhyChooseUs = whyChooseUs;

            return View();
        }

        // Hizmetler sayfası
        [HttpGet]
        public async Task<IActionResult> Services()
        {
            // Firma logolarını getir
            var companySetting = await _context.Set<CompanySetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync();

            ViewBag.CompanyLogo = companySetting?.LogoUrl;
            ViewBag.SecondCompanyLogo = companySetting?.SecondLogoUrl;

            // Aktif hizmetleri görüntülenme sırasına göre getir
            var services = await _context.Set<Service>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();

            // Aktif neden biz özelliklerini getir
            var whyChooseUs = await _context.Set<WhyChooseUs>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();

            // Verileri Hizmetler sayfasına aktar
            ViewBag.Services = services;
            ViewBag.WhyChooseUs = whyChooseUs;

            return View();
        }

        // Taşlarımız sayfası
        [HttpGet]
        public async Task<IActionResult> Stones()
        {
            // Firma logolarını getir
            var companySetting = await _context.Set<CompanySetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync();

            ViewBag.CompanyLogo = companySetting?.LogoUrl;
            ViewBag.SecondCompanyLogo = companySetting?.SecondLogoUrl;

            // Web sitesinde gösterilecek aktif taşları getir
            var stones = await _context.Set<Stone>()
                .AsNoTracking()
                .Where(x => x.IsActive && x.ShowOnWeb)
                .OrderBy(x => x.StoneId)
                .ToListAsync();

            // Taşları View'a aktar
            ViewBag.Stones = stones;

            return View();
        }

        // Galeri sayfası
        [HttpGet]
        public async Task<IActionResult> Gallery()
        {
            // Firma logolarını getir
            var companySetting = await _context.Set<CompanySetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync();

            ViewBag.CompanyLogo = companySetting?.LogoUrl;
            ViewBag.SecondCompanyLogo = companySetting?.SecondLogoUrl;

            // Web sitesinde gösterilecek aktif galeri görsellerini getir
            var galleries = await _context.Set<Gallery>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();

            // Aktif neden biz özelliklerini getir
            var whyChooseUs = await _context.Set<WhyChooseUs>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();

            // Verileri Galeri sayfasına aktar
            ViewBag.Galleries = galleries;
            ViewBag.WhyChooseUs = whyChooseUs;

            return View();
        }

        // İletişim sayfası
        [HttpGet]
        public async Task<IActionResult> Contact()
        {
            // Firma iletişim bilgilerini getir
            var companySetting = await _context.Set<CompanySetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync();

            // Firma logolarını Layout'a aktar
            ViewBag.CompanyLogo = companySetting?.LogoUrl;
            ViewBag.SecondCompanyLogo = companySetting?.SecondLogoUrl;

            // İletişim bilgilerini View'a aktar
            return View(companySetting);
        }

        // Web sitesinden gelen iletişim mesajını kaydet
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("contact")]
        public async Task<IActionResult> Contact(
            [FromForm] ContactFormInput input)
        {
            // Form alanlarını doğrula
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] =
                    "Lütfen iletişim formundaki alanları doğru ve eksiksiz doldurunuz.";

                return RedirectToAction(nameof(Contact));
            }

            // Gelen bilgilerin başındaki ve sonundaki boşlukları temizle
            string fullName = input.FullName.Trim();
            string email = input.Email.Trim();
            string subject = input.Subject.Trim();
            string messageText = input.Message.Trim();

            // Sadece boşluklardan oluşan değerleri engelle
            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(subject) ||
                string.IsNullOrWhiteSpace(messageText))
            {
                TempData["ErrorMessage"] =
                    "Lütfen tüm alanları doldurunuz.";

                return RedirectToAction(nameof(Contact));
            }

            // Yeni iletişim mesajını oluştur
            var contactMessage = new ContactMessage
            {
                FullName = fullName,
                Email = email,
                Subject = subject,
                Message = messageText,
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                IsReplied = false
            };

            // Mesajı veritabanına ekle
            _context.ContactMessages.Add(contactMessage);

            // Mesajı kaydet
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Mesajınız başarıyla gönderildi. En kısa sürede sizinle iletişime geçeceğiz.";

            return RedirectToAction(nameof(Contact));
        }
    }

    // İletişim formundan gelen verileri doğrulayan model
    public class ContactFormInput
    {
        // Mesajı gönderen kişinin adı ve soyadı
        [Required(ErrorMessage = "Ad soyad alanı zorunludur.")]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        // Mesajı gönderen kişinin e-posta adresi
        [Required(ErrorMessage = "E-posta alanı zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        // Mesajın konusu
        [Required(ErrorMessage = "Konu alanı zorunludur.")]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        // Müşterinin gönderdiği mesaj
        [Required(ErrorMessage = "Mesaj alanı zorunludur.")]
        [StringLength(5000)]
        public string Message { get; set; } = string.Empty;
    }
}

