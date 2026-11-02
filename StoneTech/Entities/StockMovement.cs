using StoneTech.Enums;

namespace StoneTech.Entities
{
    public class StockMovement
    {
        // Stok hareketinin Id
        public int StockMovementId { get; set; }

        // Hangi taşa ait olduğu
        public int StoneId { get; set; }

        // Giren veya çıkan plaka adedi
        public int Quantity { get; set; }

        // Stok hareketinin giriş veya çıkış olduğunu belirtir
        public StockMovementType MovementType { get; set; }

        // Stok hareketinin tarihi
        public DateTime MovementDate { get; set; } = DateTime.Now;

        // Stok hareketiyle ilgili açıklama
        public string? Description { get; set; }

        // Hareket bir alımdan oluştuysa alımın Id'si
        public int? PurchaseId { get; set; }

        // Hareket bir iş emrinden oluştuysa iş emrinin Id'si
        public int? WorkOrderId { get; set; }

        // İlişkili taş
        public Stone Stone { get; set; } = null!;

        // İlişkili alım
        public Purchase? Purchase { get; set; }

        // İlişkili iş emri
        public WorkOrder? WorkOrder { get; set; }
    }
}