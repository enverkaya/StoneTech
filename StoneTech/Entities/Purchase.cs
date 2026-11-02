namespace StoneTech.Entities
{
    public class Purchase
    {
        public int PurchaseId { get; set; }

        // Kullanıcının göreceği alım numarası
        // Örnek: ALM-2026-000001
        public string PurchaseNumber { get; set; } = string.Empty;

        public int SupplierId { get; set; }

        public DateTime PurchaseDate { get; set; } = DateTime.Now;

        public decimal TotalAmount { get; set; }

        public string? Description { get; set; }

        public Supplier Supplier { get; set; } = null!;

        public ICollection<PurchaseDetail> PurchaseDetails { get; set; }
            = new List<PurchaseDetail>();

        public ICollection<StockMovement> StockMovements { get; set; }
            = new List<StockMovement>();
    }
}