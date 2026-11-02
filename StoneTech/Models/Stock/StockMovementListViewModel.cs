using StoneTech.Enums;

namespace StoneTech.Models.Stock
{
    public class StockMovementListViewModel
    {
        //Stok Hareketi Id
        public int StockMovementId { get; set; }

        // Hareketin ait olduğu taş Idsi
        public int StoneId { get; set; }

        //Hareketin ait olduğu taşın adı
        public string StoneName { get; set; } = string.Empty;

        //Stok Hareketindeki palaka adedi
        public int Quantity { get; set; }

        // Stok hareketinin türü
        // Giriş, çıkış, düzeltme girişi veya düzeltme çıkışı olabilir
        public StockMovementType MovementType { get; set; }

        //Stok hareketinin gerçekleştiği tarih
        public DateTime MovementDate { get; set; }

        //Stok hareketiyle ilgili açıklama 
        public string? Description { get; set; }

        //Hareket bir alımdan oluştuysa ilgili alımın Id si
        public int? PurchaseId { get; set; }

        //Hareket bir alımdan oluştuysa kullanıcıya gözükecek alım numarası
        public string? PurchaseNumber { get; set; }

        //Hareket bir iş emrinden oluştuysa iligili iş emirinin Id'si
        public int? WorkOrderId { get; set; }


    }
}
