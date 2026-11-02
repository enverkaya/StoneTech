using StoneTech.Enums;

namespace StoneTech.Models.Payment
{
    public class PaymentDetailsViewModel
    {
        // Ödemenin veritabanı kimliği
        public int PaymentId { get; set; }

        // Ödemenin bağlı olduğu iş emrinin veritabanı kimliği
        // Yönlendirme işlemlerinde kullanılır, kullanıcıya gösterilmez
        public int WorkOrderId { get; set; }

        // Kullanıcıya gösterilecek iş emri numarası
        // Örnek: IE-2026-000001
        public string WorkOrderNumber { get; set; } = string.Empty;

        // Müşterinin veritabanı kimliği
        public int CustomerId { get; set; }

        // Müşteri adı
        public string CustomerName { get; set; } = string.Empty;

        // Ödeme tutarı
        public decimal Amount { get; set; }

        // Ödeme tarihi
        public DateTime PaymentDate { get; set; }

        // Ödeme yöntemi
        public PaymentMethod PaymentMethod { get; set; }

        // Ödeme açıklaması
        public string? Description { get; set; }

        // İş emrinin genel toplamı
        public decimal WorkOrderTotal { get; set; }

        // Bu ödeme dahil iş emrine yapılan toplam ödeme
        public decimal TotalPaid { get; set; }

        // İş emrinde kalan borç
        public decimal RemainingAmount { get; set; }
    }
}