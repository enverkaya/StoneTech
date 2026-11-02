
namespace StoneTech.Models.ContactMessage
{
    public class ContactMessageIndexViewModel
    {
        // Mesajın kimlik numarası
        public int Id { get; set; }

        // Mesajı gönderen kişinin adı ve soyadı
        public string FullName { get; set; } = string.Empty;

        // Mesajı gönderen kişinin e-posta adresi
        public string Email { get; set; } = string.Empty;

        // Mesajın konusu
        public string Subject { get; set; } = string.Empty;

        // Mesajın gönderildiği tarih
        public DateTime CreatedAt { get; set; }

        // Mesaj okundu mu?
        public bool IsRead { get; set; }

        // Mesaja cevap verildi mi?
        public bool IsReplied { get; set; }
    }
}
