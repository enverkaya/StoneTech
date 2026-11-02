
using System;
using System.ComponentModel.DataAnnotations;

namespace StoneTech.Models.ContactMessage
{
    public class ContactMessageDetailsViewModel
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
        public DateTime CreatedAt { get; set; }

        // Mesaj okundu mu?
        public bool IsRead { get; set; }

        // Mesaja e-posta ile cevap verildi mi?
        public bool IsReplied { get; set; }

        // Yönetici tarafından yazılan cevap
        [Required(ErrorMessage = "Cevap alanı boş bırakılamaz.")]
        [StringLength(5000, ErrorMessage = "Cevap en fazla 5000 karakter olabilir.")]
        public string? ReplyMessage { get; set; }

        // Cevabın gönderildiği tarih
        public DateTime? RepliedAt { get; set; }
    }
}
