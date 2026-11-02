namespace StoneTech.Models.Purchase
{
    public class PurchaseListViewModel
    {
        public int PurchaseId { get; set; }

        // Kullanıcıya gösterilecek otomatik alım numarası
        public string PurchaseNumber { get; set; } = string.Empty;

        public string SupplierName { get; set; } = string.Empty;

        public DateTime PurchaseDate { get; set; }

        public decimal TotalAmount { get; set; }

        public int DetailCount { get; set; }

        public int TotalQuantity { get; set; }

        public string? Description { get; set; }
    }
}