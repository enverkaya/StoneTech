using StoneTech.Enums;

namespace StoneTech.Models.Report
{
    public class WorkOrderReportViewModel
    {
        // Rapor başlangıç tarihi
        public DateTime? StartDate { get; set; }

        // Rapor bitiş tarihi
        public DateTime? EndDate { get; set; }

        // Seçilen iş emri durumu
        // Null olduğunda tüm durumlar gösterilir
        public WorkOrderStatus? Status { get; set; }

        // Müşteri araması
        public string? Search { get; set; }

        // Seçilen dönemdeki toplam iş emri sayısı
        public int TotalWorkOrderCount { get; set; }

        // Seçilen dönemdeki iş emirlerinin toplam tutarı
        public decimal TotalWorkOrderAmount { get; set; }

        // Seçilen dönemdeki ortalama iş emri tutarı
        public decimal AverageWorkOrderAmount { get; set; }

        // Seçilen iş emirlerine yapılan toplam ödeme
        public decimal TotalPaidAmount { get; set; }

        // Seçilen iş emirlerinin kalan toplam alacağı
        public decimal TotalRemainingAmount { get; set; }

        // İş emri durum dağılımı
        // Grafik oluşturmak için kullanılacak
        public List<WorkOrderStatusReportItemViewModel> StatusDistribution { get; set; }
            = new List<WorkOrderStatusReportItemViewModel>();

        // Rapor tablosunda gösterilecek iş emirleri
        public List<WorkOrderReportItemViewModel> WorkOrders { get; set; }
            = new List<WorkOrderReportItemViewModel>();
    }

    public class WorkOrderReportItemViewModel
    {
        // İş emrinin veritabanı kimliği
        // Detay sayfasına yönlendirme için kullanılır
        public int WorkOrderId { get; set; }

        // Kullanıcıya gösterilecek iş emri numarası
        // Örnek: IE-2026-000001
        public string WorkOrderNumber { get; set; } = string.Empty;

        // Müşterinin Id'si
        public int CustomerId { get; set; }

        // Müşteri adı veya firma unvanı
        public string CustomerName { get; set; } = string.Empty;

        // İş emrinin oluşturulma tarihi
        public DateTime CreatedDate { get; set; }

        // İş emrinin mevcut durumu
        public WorkOrderStatus Status { get; set; }

        // İş emrinin KDV hariç ara toplamı
        public decimal SubTotal { get; set; }

        // İş emrinin KDV tutarı
        public decimal VatAmount { get; set; }

        // İş emrinin KDV dahil genel toplamı
        public decimal TotalAmount { get; set; }

        // İş emrine yapılan toplam ödeme
        public decimal TotalPaid { get; set; }

        // İş emrinin kalan borcu
        public decimal RemainingAmount { get; set; }

        // İş emrindeki toplam plaka adedi
        public int TotalStoneQuantity { get; set; }

        // İş emrindeki kesim metresi
        public decimal CuttingMeter { get; set; }
    }

    public class WorkOrderStatusReportItemViewModel
    {
        // İş emri durumu
        public WorkOrderStatus Status { get; set; }

        // Bu durumdaki iş emri sayısı
        public int Count { get; set; }

        // Bu durumdaki iş emirlerinin toplam tutarı
        public decimal TotalAmount { get; set; }
    }
}