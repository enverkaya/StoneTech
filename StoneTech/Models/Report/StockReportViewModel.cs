namespace StoneTech.Models.Report
{
    public class StockReportViewModel
    {
        // Rapor başlangıç tarihi
        public DateTime? StartDate { get; set; }

        // Rapor bitiş tarihi
        public DateTime? EndDate { get; set; }

        // Taş adı veya renk arama metni
        public string? Search { get; set; }

        // Aktif veya pasif kayıt filtresi
        public bool? IsActive { get; set; }

        // Filtreye uyan toplam taş çeşidi
        public int StoneCount { get; set; }

        // Güncel toplam plaka stoğu
        public int TotalCurrentStock { get; set; }

        // Seçilen dönemdeki toplam stok girişi
        public int TotalEntries { get; set; }

        // Seçilen dönemdeki toplam stok çıkışı
        public int TotalExits { get; set; }

        // Seçilen dönemdeki toplam stok düzeltmesi
        public int TotalAdjustments { get; set; }

        // Seçilen dönemdeki toplam iş emri iadesi
        public int TotalReturns { get; set; }

        // Stok raporu detayları
        public List<StockReportItemViewModel> Items { get; set; } = new();
    }

    public class StockReportItemViewModel
    {
        // Taş Id
        public int StoneId { get; set; }

        // Taş adı
        public string StoneName { get; set; } = string.Empty;

        // Taş rengi
        public string? Color { get; set; }

        // Taşın aktiflik durumu
        public bool IsActive { get; set; }

        // Güncel plaka stoğu
        public int CurrentStock { get; set; }

        // Seçilen dönemdeki toplam stok girişi
        public int TotalEntry { get; set; }

        // Seçilen dönemdeki toplam stok çıkışı
        public int TotalExit { get; set; }

        // Seçilen dönemdeki düzeltme girişi
        public int AdjustmentIn { get; set; }

        // Seçilen dönemdeki düzeltme çıkışı
        public int AdjustmentOut { get; set; }

        // Seçilen dönemdeki iş emri iadesi
        public int WorkOrderReturn { get; set; }
    }
}