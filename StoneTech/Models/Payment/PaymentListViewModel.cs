namespace StoneTech.Models.Payment
{
    public class PaymentListViewModel
    {
        // İş emrinin veritabanı kimliği
        // Yönlendirme işlemlerinde kullanılır, kullanıcıya gösterilmez
        public int WorkOrderId { get; set; }

        // Kullanıcıya gösterilecek iş emri numarası
        // Örnek: IE-2026-000001
        public string WorkOrderNumber { get; set; } = string.Empty;

        // Müşterinin veritabanı kimliği
        // Yönlendirme işlemlerinde kullanılır, kullanıcıya gösterilmez
        public int CustomerId { get; set; }

        // Müşteri adı
        public string CustomerName { get; set; } = string.Empty;

        // İş emrinin oluşturulma tarihi
        public DateTime WorkOrderDate { get; set; }

        // İş emrinin genel toplamı
        public decimal TotalAmount { get; set; }

        // İş emrine bugüne kadar yapılan toplam ödeme
        public decimal TotalPaid { get; set; }

        // İş emrinde kalan borç
        public decimal RemainingAmount { get; set; }

        // İş emrine ait ödeme sayısı
        public int PaymentCount { get; set; }
    }
}