namespace StoneTech.Models.Report
{
    public class ReportIndexViewModel
    {
        // Rapor başlangıç tarihi
        public DateTime? StartDate { get; set; }

        // Rapor bitiş tarihi
        public DateTime? EndDate { get; set; }

        // Seçilen dönemdeki geçerli iş emirlerinin toplam tutarı
        public decimal TotalWorkOrderAmount { get; set; }

        // Seçilen dönemde alınan toplam ödeme
        public decimal TotalPaymentAmount { get; set; }

        // Seçilen dönemdeki iş emirlerinden kalan toplam alacak
        public decimal TotalReceivableAmount { get; set; }

        // Seçilen dönemde yapılan toplam alım tutarı
        public decimal TotalPurchaseAmount { get; set; }

        // Seçilen dönemde oluşturulan geçerli iş emri sayısı
        public int WorkOrderCount { get; set; }

        // Seçilen dönemde alınan ödeme sayısı
        public int PaymentCount { get; set; }

        // Seçilen dönemde yapılan alım sayısı
        public int PurchaseCount { get; set; }

        // Borcu bulunan müşteri sayısı
        public int DebtorCustomerCount { get; set; }
    }
}