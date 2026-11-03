
namespace StoneTech.Entities
{
    public class ContactMessage
    {
        // Mesajın kimlik numarası
        public int Id { get; set; }

        // Mesajı gönderen kişinin adı ve soyadı
        public string FullName { get; set; } = string.Empty;

        // Mesajı gönderen kişinin e-posta adresi
        public string Email { get; set; } = string.Empty;

        // Mesajın konusu
        public string Subject { get; set; } = string.Empty;

        // Müşterinin gönderdiği mesaj
        public string Message { get; set; } = string.Empty;

        // Mesajın gönderildiği tarih
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Mesaj okundu mu?
        public bool IsRead { get; set; } = false;

        // Mesaja e-posta ile yanıt gönderildi mi?
        public bool IsReplied { get; set; } = false;

        // E-posta yanıtı gönderiminin başlatıldığını veya sonucunun belirsiz olduğunu belirtir
        public bool IsReplyProcessing { get; set; } = false;

        // Yönetici tarafından gönderilen yanıt
        public string? ReplyMessage { get; set; }

        // Yanıtın gönderildiği tarih
        public DateTime? RepliedAt { get; set; }
    }
}
