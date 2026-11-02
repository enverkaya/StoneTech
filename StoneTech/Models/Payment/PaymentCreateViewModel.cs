using System.ComponentModel.DataAnnotations;
using StoneTech.Enums;

namespace StoneTech.Models.Payment
{
    public class PaymentCreateViewModel
    {
        // Ödeme yapılacak iş emrinin veritabanı kimliği
        // Form işlemlerinde kullanılır, kullanıcıya gösterilmez
        [Required(ErrorMessage = "İş emri seçilmelidir.")]
        [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir iş emri seçiniz.")]
        public int WorkOrderId { get; set; }

        // Kullanıcıya gösterilecek iş emri numarası
        // Örnek: IE-2026-000001
        public string WorkOrderNumber { get; set; } = string.Empty;

        // Ödeme tutarı
        [Required(ErrorMessage = "Ödeme tutarı girilmelidir.")]
        public decimal Amount { get; set; }

        // Ödeme tarihi
        [Required(ErrorMessage = "Ödeme tarihi seçilmelidir.")]
        [DataType(DataType.Date)]
        public DateTime PaymentDate { get; set; } = DateTime.Today;

        // Ödeme yöntemi
        [Required(ErrorMessage = "Ödeme yöntemi seçilmelidir.")]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        // Ödemeyle ilgili açıklama
        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        public string? Description { get; set; }

        // Ekranda gösterilecek müşteri adı
        public string CustomerName { get; set; } = string.Empty;

        // İş emrinin genel toplamı
        public decimal WorkOrderTotal { get; set; }

        // Daha önce ödenen toplam tutar
        public decimal TotalPaid { get; set; }

        // Kalan borç
        public decimal RemainingAmount { get; set; }
    }
}