
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

        // Gelen mesajları listeler
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            int page = 1,
            int pageSize = 10)
        {
            // Sayfa numarasının geçerli olmasını sağlar
            if (page < 1)
            {
                page = 1;
            }

            // Sayfa başına gösterilecek kayıt sayısını kontrol eder
            if (pageSize != 5 &&
                pageSize != 10 &&
                pageSize != 20 &&
                pageSize != 50)
            {
                pageSize = 10;
            }

            // Arama metnini düzenler
            search = search?.Trim();

            // Mesaj sorgusunu oluşturur
            var query = _context.ContactMessages
                .AsNoTracking()
                .AsQueryable();

            // Ad soyad, e-posta veya konuya göre arama yapar
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.FullName.Contains(search) ||
                    x.Email.Contains(search) ||
                    x.Subject.Contains(search));
            }

            // Filtreye uygun toplam mesaj sayısını hesaplar
            int totalCount = await query.CountAsync();

            // Toplam sayfa sayısını hesaplar
            int totalPages = (int)Math.Ceiling(
                totalCount / (double)pageSize);

            // İstenen sayfanın toplam sayfa sayısını aşmasını engeller
            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            // Hiç kayıt yoksa ilk sayfayı gösterir
            if (totalPages == 0)
            {
                page = 1;
            }

            // Mesajları en yeniden en eskiye sıralar ve sayfalar
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

            // Arama bilgisini View'a gönderir
            ViewBag.Search = search ?? "";

            // Aktif sayfa numarasını View'a gönderir
            ViewBag.Page = page;

            // Sayfa başına kayıt sayısını View'a gönderir
            ViewBag.PageSize = pageSize;

            // Toplam sayfa sayısını View'a gönderir
            ViewBag.TotalPages = totalPages;

            // Toplam mesaj sayısını View'a gönderir
            ViewBag.TotalCount = totalCount;

            return View(messages);
        }

        // Gelen mesajın detaylarını yalnızca görüntüler
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            // Mesajı veritabanından salt okunur şekilde getirir
            var message = await _context.ContactMessages
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            // Mesaj bulunamazsa 404 döndürür
            if (message == null)
            {
                return NotFound();
            }

            // Mesaj bilgilerini Details modeline aktarır
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

        // Admin isteğiyle mesajı okundu olarak işaretler
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            // Yalnızca henüz okunmamış mesajın durumunu değiştirir
            int affectedRows = await _context.ContactMessages
                .Where(x => x.Id == id && !x.IsRead)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.IsRead, true));

            // Güncelleme yapılmadıysa mesajın varlığını kontrol eder
            if (affectedRows == 0)
            {
                bool exists = await _context.ContactMessages
                    .AnyAsync(x => x.Id == id);

                // Mesaj bulunamadıysa 404 döndürür
                if (!exists)
                {
                    return NotFound();
                }
            }

            // İşlem sonucunu kullanıcıya bildirir
            TempData["Success"] = "Mesaj okundu olarak işaretlendi.";

            // Mesaj detayına geri döner
            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // Müşteriye e-posta ile cevap gönderir
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(
            ContactMessageDetailsViewModel model)
        {
            // Cevap alanındaki gereksiz boşlukları temizler
            string replyText = model.ReplyMessage?.Trim() ?? string.Empty;

            // Cevap alanının boş olmasını engeller
            if (string.IsNullOrWhiteSpace(replyText))
            {
                TempData["Error"] = "Lütfen cevap mesajı yazınız.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = model.Id });
            }

            // Cevabın veritabanı sınırını aşmasını engeller
            if (replyText.Length > 5000)
            {
                TempData["Error"] = "Cevap en fazla 5000 karakter olabilir.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = model.Id });
            }

            // Mesajı veritabanından getirir
            var message = await _context.ContactMessages
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == model.Id);

            // Mesaj bulunamadıysa 404 döndürür
            if (message == null)
            {
                return NotFound();
            }

            // Mesajı yalnızca cevaplanmamış ve işleme alınmamışsa kilitler
            // ExecuteUpdateAsync işlemi veritabanında atomik olarak çalışır
            int affectedRows = await _context.ContactMessages
                .Where(x =>
                    x.Id == model.Id &&
                    !x.IsReplied &&
                    !x.IsReplyProcessing)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.IsReplyProcessing, true));

            // Başka bir istek mesajı işleme aldıysa tekrar e-posta göndermez
            if (affectedRows == 0)
            {
                TempData["Error"] =
                    "Bu mesaj daha önce cevaplanmış veya cevap gönderimi başlatılmış.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = model.Id });
            }

            // Müşteriye gönderilecek e-posta konusu
            string subject = $"KAYA Ceramic & Tile - {message.Subject}";

            // Müşteriye gönderilecek e-posta içeriği
            string emailBody =
                $"Sayın {message.FullName},\n\n" +
                $"{replyText}\n\n" +
                "Saygılarımızla,\n" +
                "KAYA Ceramic & Tile";

            try
            {
                // Müşterinin e-posta adresine cevap gönderir
                await _mailService.SendEmailAsync(
                    message.Email,
                    subject,
                    emailBody);

                // Gönderilen cevabı veritabanına kaydeder
                int updatedRows = await _context.ContactMessages
                    .Where(x =>
                        x.Id == model.Id &&
                        x.IsReplyProcessing &&
                        !x.IsReplied)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.ReplyMessage, replyText)
                        .SetProperty(x => x.IsReplied, true)
                        .SetProperty(x => x.RepliedAt, DateTime.UtcNow)
                        .SetProperty(x => x.IsReplyProcessing, false));

                // Cevap durumunun veritabanına kaydedildiğini doğrular
                if (updatedRows != 1)
                {
                    throw new InvalidOperationException(
                        "E-posta gönderildi ancak cevap durumu veritabanına kaydedilemedi.");
                }

                TempData["Success"] =
                    "Cevabınız müşterinin e-posta adresine gönderildi.";
            }
            catch (Exception ex)
            {
                // Hata ayrıntılarını sistem kayıtlarına yazar
                _logger.LogError(
                    ex,
                    "İletişim mesajına cevap gönderilirken hata oluştu. Mesaj ID: {MessageId}",
                    model.Id);

                // Gönderimin sonucu belirsiz olabileceği için kilidi kaldırmaz
                // Böylece aynı e-postanın yanlışlıkla tekrar gönderilmesini önler
                TempData["Error"] =
                    "Cevap gönderimi sırasında hata oluştu. " +
                    "E-posta gönderilmiş olabilir. Tekrar göndermeden önce işlem durumunu kontrol ediniz.";
            }

            return RedirectToAction(
                nameof(Details),
                new { id = model.Id });
        }
    }
}
