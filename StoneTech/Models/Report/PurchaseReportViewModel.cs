namespace StoneTech.Models.Report
{
    public class PurchaseReportViewModel
    {
        // Rapor başlangıç tarihi
        public DateTime? StartDate { get; set; }

        // Rapor bitiş tarihi
        public DateTime? EndDate { get; set; }

        // Tedarikçi filtresi
        public int? SupplierId { get; set; }

        // Alım numarası veya tedarikçi arama metni
        public string? Search { get; set; }

        // Toplam alım sayısı
        public int PurchaseCount { get; set; }

        // Raporda bulunan toplam tedarikçi sayısı
        public int SupplierCount { get; set; }

        // Toplam alınan plaka adedi
        public int TotalPlateCount { get; set; }

        // Toplam alım tutarı
        public decimal TotalAmount { get; set; }

        // Filtrede kullanılacak tedarikçiler
        public List<ReportOptionViewModel> Suppliers { get; set; } = new();

        // Alım raporu detayları
        public List<PurchaseReportItemViewModel> Items { get; set; } = new();
    }

    public class PurchaseReportItemViewModel
    {
        // Alım Id
        public int PurchaseId { get; set; }

        // Alım numarası
        public string PurchaseNumber { get; set; } = string.Empty;

        // Tedarikçi Id
        public int SupplierId { get; set; }

        // Tedarikçi adı
        public string SupplierName { get; set; } = string.Empty;

        // Alım tarihi
        public DateTime PurchaseDate { get; set; }

        // Alımdaki detay satırı sayısı
        public int DetailCount { get; set; }

        // Toplam alınan plaka adedi
        public int TotalQuantity { get; set; }

        // Alımın toplam tutarı
        public decimal TotalAmount { get; set; }

        // Alım açıklaması
        public string? Description { get; set; }
    }
}