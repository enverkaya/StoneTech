using StoneTech.Enums;

namespace StoneTech.Models.WorkOrder
{
    public class WorkOrderDetailsViewModel
    {
        // İş emrinin Id'si
        public int WorkOrderId { get; set; }

        // Kullanıcıya gösterilecek iş emri numarası
        // Örnek: IE-2026-000001
        public string WorkOrderNumber { get; set; } = string.Empty;

        // Müşterinin Id'si
        public int CustomerId { get; set; }

        // Müşterinin adı soyadı veya firma adı
        public string CustomerName { get; set; } = string.Empty;

        // Müşterinin telefon numarası
        public string Phone { get; set; } = string.Empty;

        // İş emrinin oluşturulma tarihi
        public DateTime CreatedDate { get; set; }

        // İş emrinin mevcut durumu
        public WorkOrderStatus Status { get; set; }

        // İş emriyle ilgili genel açıklama
        public string? Description { get; set; }

        // Atölyede yapılan toplam kesim metresi
        public decimal CuttingMeter { get; set; }

        // Kesimin metre başına KDV hariç işçilik fiyatı
        public decimal CuttingPricePerMeter { get; set; }

        // Toplam kesim işçilik tutarı
        public decimal CuttingTotal { get; set; }

        // İş emrinde kullanılan taşların toplam KDV hariç tutarı
        public decimal StoneTotal { get; set; }

        // Taş ve kesim işçiliğinin KDV hariç ara toplamı
        public decimal SubTotal { get; set; }

        // İş emrine uygulanan KDV oranı
        public decimal VatRate { get; set; }

        // İş emrinin hesaplanan KDV tutarı
        public decimal VatAmount { get; set; }

        // KDV dahil genel toplam
        public decimal TotalAmount { get; set; }

        // İş emrine yapılan toplam ödeme
        public decimal TotalPaid { get; set; }

        // İş emrinin kalan borcu
        public decimal RemainingAmount { get; set; }

        // İş emrinde kullanılan taşların listesi
        public List<WorkOrderDetailViewModel> Details { get; set; }
            = new List<WorkOrderDetailViewModel>();

        // İş emrine yapılan ödemelerin listesi
        public List<WorkOrderPaymentViewModel> Payments { get; set; }
            = new List<WorkOrderPaymentViewModel>();
    }

    public class WorkOrderPaymentViewModel
    {
        // Ödemenin Id'si
        public int PaymentId { get; set; }

        // Ödeme tutarı
        public decimal Amount { get; set; }

        // Ödeme tarihi
        public DateTime PaymentDate { get; set; }

        // Ödeme yöntemi
        public PaymentMethod PaymentMethod { get; set; }

        // Ödemeyle ilgili açıklama
        public string? Description { get; set; }
    }
}