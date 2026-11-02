namespace StoneTech.Models.Purchase
{
    public class PurchaseDetailsViewModel
    {
        public int PurchaseId { get; set; }

        // Kullanıcıya gösterilecek otomatik alım numarası
        public string PurchaseNumber { get; set; } = string.Empty;

        public string SupplierName { get; set; } = string.Empty;

        public string? SupplierPhone { get; set; }

        public DateTime PurchaseDate { get; set; }

        public decimal TotalAmount { get; set; }

        public string? Description { get; set; }

        public List<PurchaseDetailsItemViewModel> Details { get; set; }
            = new List<PurchaseDetailsItemViewModel>();
    }

    public class PurchaseDetailsItemViewModel
    {
        public int StoneId { get; set; }

        public string StoneName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal LineTotal { get; set; }
    }
}