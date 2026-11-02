
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoneTech.Data;
using StoneTech.Models.ContactMessage;
using StoneTech.Services;

namespace StoneTech.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ContactMessageController : Controller
    {
        // Veritabanı bağlantısı
        private readonly AppDbContext _context;

        // E-posta gönderme servisi
        private readonly IMailService _mailService;

        // Hata kayıtlarını tutan servis
        private readonly ILogger<ContactMessageController> _logger;

        // Constructor
        public ContactMessageController(
            AppDbContext context,
            IMailService mailService,
            ILogger<ContactMessageController> logger)
        {
            _context = context;
            _mailService = mailService;
            _logger = logger;
        }


        // Gelen mesajları listele
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            int page = 1,
            int pageSize = 10)
        {
            // Sayfa numarasının geçerli olmasını sağla
            if (page < 1)
            {
                page = 1;
            }

            // Sayfa başına gösterilecek kayıt sayısını kontrol et
            if (pageSize != 5 &&
                pageSize != 10 &&
                pageSize != 20 &&
                pageSize != 50)
            {
                pageSize = 10;
            }

            // Arama metnini düzenle
            search = search?.Trim();

            // Mesaj sorgusunu oluştur
            var query = _context.ContactMessages
                .AsNoTracking()
                .AsQueryable();

            // Ad soyad, e-posta veya konuya göre arama yap
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.FullName.Contains(search) ||
                    x.Email.Contains(search) ||
                    x.Subject.Contains(search));
            }

            // Filtreye uygun toplam mesaj sayısını hesapla
            int totalCount = await query.CountAsync();

            // Toplam sayfa sayısını hesapla
            int totalPages = (int)Math.Ceiling(
                totalCount / (double)pageSize);

            // İstenen sayfa toplam sayfa sayısını aşmasın
            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            // Hiç kayıt yoksa ilk sayfayı göster
            if (totalPages == 0)
            {
                page = 1;
            }

            // Mesajları en yeniden en eskiye sırala ve sayfala
            var messages = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ContactMessageIndexViewModel
                {
                    Id = x.Id,
                    FullName = x.FullName,
                    Email = x.Email,
                    Subject = x.Subject,
                    CreatedAt = x.CreatedAt,
                    IsRead = x.IsRead,
                    IsReplied = x.IsReplied
                })
                .ToListAsync();

            // Arama bilgisini View'a gönder
            ViewBag.Search = search ?? "";

            // Aktif sayfa numarasını View'a gönder
            ViewBag.Page = page;

            // Sayfa başına kayıt sayısını View'a gönder
            ViewBag.PageSize = pageSize;

            // Toplam sayfa sayısını View'a gönder
            ViewBag.TotalPages = totalPages;

            // Toplam mesaj sayısını View'a gönder
            ViewBag.TotalCount = totalCount;

            return View(messages);
        }


        // Gelen mesajın detaylarını görüntüle
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            // Mesajı veritabanından getir
            var message = await _context.ContactMessages
                .FirstOrDefaultAsync(x => x.Id == id);

            // Mesaj bulunamazsa 404 döndür
            if (message == null)
            {
                return NotFound();
            }

            // Mesaj ilk kez açılıyorsa okundu olarak işaretle
            if (!message.IsRead)
            {
                message.IsRead = true;

                await _context.SaveChangesAsync();
            }

            // Mesaj bilgilerini Details modeline aktar
            var model = new ContactMessageDetailsViewModel
            {
                Id = message.Id,
                FullName = message.FullName,
                Email = message.Email,
                Subject = message.Subject,
                Message = message.Message,
                CreatedAt = message.CreatedAt,
                IsRead = message.IsRead,
                IsReplied = message.IsReplied,
                ReplyMessage = message.ReplyMessage,
                RepliedAt = message.RepliedAt
            };

            return View(model);
        }

        // Müşteriye e-posta ile cevap gönder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(
            ContactMessageDetailsViewModel model)
        {
            // Gelen mesajı veritabanından getir
            var message = await _context.ContactMessages
                .FirstOrDefaultAsync(x => x.Id == model.Id);

            // Mesaj bulunamazsa 404 döndür
            if (message == null)
            {
                return NotFound();
            }

            // Cevap alanının boş olmasını engelle
            if (string.IsNullOrWhiteSpace(model.ReplyMessage))
            {
                TempData["Error"] = "Lütfen cevap mesajı yazınız.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = message.Id });
            }

            // Cevabın veritabanı sınırını aşmasını engelle
            if (model.ReplyMessage.Length > 5000)
            {
                TempData["Error"] =
                    "Cevap en fazla 5000 karakter olabilir.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = message.Id });
            }

            // Aynı mesaja tekrar cevap gönderilmesini engelle
            if (message.IsReplied)
            {
                TempData["Error"] =
                    "Bu mesaja daha önce cevap verilmiş.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = message.Id });
            }

            // Müşteriye gönderilecek e-posta konusu
            string subject = $"KAYA Ceramic & Tile - {message.Subject}";

            // Müşteriye gönderilecek e-posta içeriği
            string emailBody =
                $"Sayın {message.FullName},\n\n" +
                $"{model.ReplyMessage.Trim()}\n\n" +
                "Saygılarımızla,\n" +
                "KAYA Ceramic & Tile";

            try
            {
                // Müşterinin e-posta adresine cevap gönder
                await _mailService.SendEmailAsync(
                    message.Email,
                    subject,
                    emailBody);

                // Gönderilen cevabı veritabanına kaydet
                message.ReplyMessage = model.ReplyMessage.Trim();

                // Mesajı cevaplandı olarak işaretle
                message.IsReplied = true;

                // Cevap gönderim tarihini kaydet
                message.RepliedAt = DateTime.UtcNow;

                // Değişiklikleri veritabanına kaydet
                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Cevabınız müşterinin e-posta adresine gönderildi.";
            }
            catch (Exception ex)
            {
                // Hata ayrıntılarını sistem kayıtlarına yaz
                _logger.LogError(
                    ex,
                    "İletişim mesajına cevap gönderilirken hata oluştu. Mesaj ID: {MessageId}",
                    message.Id);

                TempData["Error"] =
                    "Cevap gönderilirken veya kaydedilirken hata oluştu. " +
                    "Lütfen işlem durumunu kontrol ediniz.";
            }

            return RedirectToAction(
                nameof(Details),
                new { id = message.Id });
        }
    }
}
