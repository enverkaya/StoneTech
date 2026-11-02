using System.ComponentModel.DataAnnotations;

namespace StoneTech.Models.Invoice
{
    public class InvoiceCreateViewModel
    {
        // Faturanın oluşturulacağı iş emrinin benzersiz kimliği
        [Required(ErrorMessage = "İş emri seçimi zorunludur.")]
        [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir iş emri seçiniz.")]
        public int WorkOrderId { get; set; }

        // Sistem tarafından otomatik oluşturulan fatura numarası
        // Kullanıcı bu alanı elle değiştirmez
        // Örnek: FAT-2026-000001
        public string InvoiceNumber { get; set; } = string.Empty;

        // Faturanın düzenlenme tarihi
        [Required(ErrorMessage = "Fatura tarihi zorunludur.")]
        public DateTime InvoiceDate { get; set; } = DateTime.Today;

        // Faturayla ilgili isteğe bağlı açıklama
        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        public string? Description { get; set; }
    }
}
