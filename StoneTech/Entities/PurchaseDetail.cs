
namespace StoneTech.Entities
{
    public class PurchaseDetail
    {
        // Alım detayının benzersiz kimliği
        public int PurchaseDetailId { get; set; }

        // Hangi alıma ait olduğu
        public int PurchaseId { get; set; }

        // Alınan taş
        public int StoneId { get; set; }

        // Alınan plaka adedi
        public int Quantity { get; set; }

        // Plaka başına alış fiyatı
        public decimal UnitPrice { get; set; }

        // İlişkili alım
        public Purchase Purchase { get; set; } = null!;

        // İlişkili taş
        public Stone Stone { get; set; } = null!;
    }
}