
namespace StoneTech.Models.Stone
{
    public class StoneListViewModel
    {
        // Taşın Id bilgisi
        public int StoneId { get; set; }

        // Taşın adı
        public string StoneName { get; set; } = string.Empty;

        // Taşın rengi
        public string? Color { get; set; }

        // Güncel alış fiyatı
        public decimal PurchasePrice { get; set; }

        // Güncel satış fiyatı
        public decimal SalePrice { get; set; }

        // Taşın görsel yolu
        public string? ImageUrl { get; set; }

        // Taş sistemde aktif mi?
        public bool IsActive { get; set; }

        // Taş web sitesinde gösterilsin mi?
        public bool ShowOnWeb { get; set; }

        // Hareketlerden hesaplanan mevcut plaka stoğu
        public int StockQuantity { get; set; }
    }
}
