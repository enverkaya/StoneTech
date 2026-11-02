using StoneTech.Enums;

namespace StoneTech.Models.Report
{
    public class CustomerReportViewModel
    {
        // Rapor başlangıç tarihi
        public DateTime? StartDate { get; set; }

        // Rapor bitiş tarihi
        public DateTime? EndDate { get; set; }

        // Müşteri tipi filtresi
        public CustomerType? CustomerType { get; set; }

        // Müşteri arama metni
        public string? Search { get; set; }

        // Toplam müşteri sayısı
        public int CustomerCount { get; set; }

        // Toplam iş emri sayısı
        public int WorkOrderCount { get; set; }

        // Toplam iş hacmi
        public decimal TotalBusinessVolume { get; set; }

        // Toplam tahsil edilen tutar
        public decimal TotalPaid { get; set; }

        // Toplam kalan alacak
        public decimal TotalRemaining { get; set; }

        // Müşteri raporu detayları
        public List<CustomerReportItemViewModel> Items { get; set; } = new();
    }

    public class CustomerReportItemViewModel
    {
        // Müşteri Id
        public int CustomerId { get; set; }

        // Müşteri adı
        public string CustomerName { get; set; } = string.Empty;

        // Müşteri tipi
        public CustomerType CustomerType { get; set; }

        // Telefon numarası
        public string Phone { get; set; } = string.Empty;

        // Müşterinin aktiflik durumu
        public bool IsActive { get; set; }

        // Müşterinin toplam iş emri sayısı
        public int WorkOrderCount { get; set; }

        // Müşterinin toplam iş tutarı
        public decimal TotalAmount { get; set; }

        // Müşteriden tahsil edilen toplam tutar
        public decimal TotalPaid { get; set; }

        // Müşterinin kalan toplam borcu
        public decimal RemainingAmount { get; set; }
    }
}