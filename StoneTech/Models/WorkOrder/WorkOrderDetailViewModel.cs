namespace StoneTech.Models.WorkOrder
{
    public class WorkOrderDetailViewModel
    {
        // İş emri detayının Id'si
        public int WorkOrderDetailId { get; set; }

        // Kullanılan taşın Id'si
        public int StoneId { get; set; }

        // Kullanılan taşın adı
        public string StoneName { get; set; } = string.Empty;

        // Kullanılan plaka adedi
        public int Quantity { get; set; }

        // Plaka başına uygulanan satış fiyatı
        public decimal UnitPrice { get; set; }

        // Bu taş kaleminin toplam satış tutarı
        public decimal LineTotal { get; set; }

        // Taş için girilen işlem veya ölçü bilgisi
        public string? Description { get; set; }
    }
}