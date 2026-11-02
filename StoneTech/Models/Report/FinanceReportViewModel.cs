namespace StoneTech.Models.Report
{
    public class FinanceReportViewModel
    {
        // Rapor başlangıç tarihi
        public DateTime? StartDate { get; set; }

        // Rapor bitiş tarihi
        public DateTime? EndDate { get; set; }

        // Müşteri arama metni
        public string? Search { get; set; }

        // Seçilen dönemdeki toplam iş emri tutarı
        public decimal TotalWorkOrderAmount { get; set; }

        // Seçilen iş emirlerine yapılan toplam ödeme
        public decimal TotalPaidAmount { get; set; }

        // Seçilen iş emirlerinin kalan toplam alacağı
        public decimal TotalRemainingAmount { get; set; }

        // Toplam tahsilat oranı
        public decimal CollectionRate { get; set; }

        // Finans raporu müşteri detayları
        public List<FinanceReportItemViewModel> Items { get; set; }
            = new List<FinanceReportItemViewModel>();
    }

    public class FinanceReportItemViewModel
    {
        // Müşteri Id
        public int CustomerId { get; set; }

        // Müşteri adı
        public string CustomerName { get; set; } = string.Empty;

        // Müşteriye ait iş emri sayısı
        public int WorkOrderCount { get; set; }

        // Müşterinin toplam iş emri tutarı
        public decimal TotalAmount { get; set; }

        // Müşteriden alınan toplam ödeme
        public decimal TotalPaid { get; set; }

        // Müşteriden kalan toplam alacak
        public decimal RemainingAmount { get; set; }
    }
}