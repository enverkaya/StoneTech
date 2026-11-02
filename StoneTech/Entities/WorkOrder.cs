using StoneTech.Enums;

namespace StoneTech.Entities
{
    public class WorkOrder
    {
        // İş emrinin benzersiz kimliği
        public int WorkOrderId { get; set; }

        // Kullanıcıya gösterilecek benzersiz iş emri numarası
        // Örnek: IE-2026-000001
        public string WorkOrderNumber { get; set; } = string.Empty;

        // İş emrinin ait olduğu müşterinin Id'si
        public int CustomerId { get; set; }

        // İş emrinin oluşturulma tarihi
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Atölyede yapılacak toplam kesim metresi
        public decimal CuttingMeter { get; set; }

        // Kesimin metre başına KDV hariç işçilik fiyatı
        public decimal CuttingPricePerMeter { get; set; }

        // Taş ve işçilik toplamının KDV hariç tutarı
        public decimal SubTotal { get; set; }

        // İş emrine uygulanacak KDV oranı
        // Örnek: %20 KDV için 20 değeri tutulur
        public decimal VatRate { get; set; } = 20m;

        // İş emrinin hesaplanan KDV tutarı
        public decimal VatAmount { get; set; }

        // KDV dahil müşteriden tahsil edilecek genel toplam
        public decimal TotalAmount { get; set; }

        // İş emrinin mevcut durumu
        public WorkOrderStatus Status { get; set; }
            = WorkOrderStatus.PendingApproval;

        // İş emriyle ilgili açıklama
        public string? Description { get; set; }

        // İş emrinin ait olduğu müşteri
        public Customer Customer { get; set; } = null!;

        // İş emrinde kullanılan taşlar
        public ICollection<WorkOrderDetail> WorkOrderDetails { get; set; }
            = new List<WorkOrderDetail>();

        // İş emrine yapılan ödemeler
        public ICollection<Payment> Payments { get; set; }
            = new List<Payment>();

        // İş emrinin oluşturduğu stok hareketleri
        public ICollection<StockMovement> StockMovements { get; set; }
            = new List<StockMovement>();
    }
}