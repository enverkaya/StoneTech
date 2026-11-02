using StoneTech.Enums;

namespace StoneTech.Models.Customer
{
    public class CustomerDebtorViewModel
    {
        // Müşterinin veritabanı kimliği
        public int CustomerId { get; set; }

        // Müşteri adı
        public string CustomerName { get; set; } = string.Empty;

        // Müşteri telefon numarası
        public string Phone { get; set; } = string.Empty;

        // Müşterinin toplam borçlandırılan iş tutarı
        public decimal TotalAmount { get; set; }

        // Müşteriden bugüne kadar alınan toplam ödeme
        public decimal TotalPaid { get; set; }

        // Müşterinin kalan toplam borcu
        public decimal RemainingDebt { get; set; }

        // Müşterinin halen borcu bulunan iş emirleri
        public List<CustomerDebtorWorkOrderViewModel> WorkOrders { get; set; } = new();
    }

    public class CustomerDebtorWorkOrderViewModel
    {
        // İş emrinin veritabanı kimliği
        public int WorkOrderId { get; set; }

        // Kullanıcıya gösterilen iş emri numarası
        public string WorkOrderNumber { get; set; } = string.Empty;

        // İş emrinin oluşturulma tarihi
        public DateTime CreatedDate { get; set; }

        // İş emrinin toplam tutarı
        public decimal TotalAmount { get; set; }

        // Bu iş emrine alınan toplam ödeme
        public decimal TotalPaid { get; set; }

        // Bu iş emrinin kalan borcu
        public decimal RemainingAmount { get; set; }

        // İş emrinin mevcut durumu
        public WorkOrderStatus Status { get; set; }
    }
}