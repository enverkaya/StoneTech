namespace StoneTech.Entities
{
    public class Stone
    {
        // Taş Id
        public int StoneId { get; set; }

        // Taşın İsmi
        public string StoneName { get; set; } = string.Empty;

        // Taşın Rengi
        public string? Color { get; set; }

        // Taşın Alış Fiyatı
        public decimal PurchasePrice { get; set; }

        // Taşın Satış Fiyatı
        public decimal SalePrice { get; set; }

        // Taşa ait görselin dosya yolu
        public string? ImageUrl { get; set; }

        // Taş Aktif Mi?
        public bool IsActive { get; set; } = true;

        // Taşın web sitesinde gösterilecek açıklaması
        public string? WebDescription { get; set; }

        // Taş web sitesinde gösterilsin mi?
        public bool ShowOnWeb { get; set; } = false;

        // Taşa ait stok hareketleri
        public ICollection<StockMovement> StockMovements { get; set; }
            = new List<StockMovement>();

        // Taşın kullanıldığı iş emri detayları
        public ICollection<WorkOrderDetail> WorkOrderDetails { get; set; }
            = new List<WorkOrderDetail>();

        // Taşın bulunduğu alım detayları
        public ICollection<PurchaseDetail> PurchaseDetails { get; set; }
            = new List<PurchaseDetail>();
    }
}