namespace StoneTech.Entities
{
    public class WorkOrderDetail
    {
        // İş emri detayının benzersiz kimliği
        public int WorkOrderDetailId { get; set; }

        // Hangi iş emrine ait olduğu
        public int WorkOrderId { get; set; }

        // Kullanılacak taşın Id'si
        public int StoneId { get; set; }

        // Kullanılacak plaka adedi
        public int Quantity { get; set; }

        // Plaka başına satış fiyatı
        public decimal UnitPrice { get; set; }

        // Yapılacak işlem, ölçü veya detay bilgisi
        public string? Description { get; set; }

        // İlişkili iş emri
        public WorkOrder WorkOrder { get; set; } = null!;

        // İlişkili taş
        public Stone Stone { get; set; } = null!;
    }
}