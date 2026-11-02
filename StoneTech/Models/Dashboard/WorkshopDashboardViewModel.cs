namespace StoneTech.Models.Dashboard
{
    public class WorkshopDashboardViewModel
    {
        // Dashboard'da karşılanacak giriş yapmış atölye personelinin adını ve soyadını tutar
        public string CurrentUserFullName { get; set; } = string.Empty;

        // Onaylanmış ve atölyeye başlamayı bekleyen iş emri sayısını tutar
        public int ApprovedWorkOrderCount { get; set; }

        // Şu anda atölyede işlem gören iş emri sayısını tutar
        public int InWorkshopCount { get; set; }

        // Bu ay tamamlanan iş emri sayısını tutar
        public int MonthlyCompletedCount { get; set; }

        // Atölyeye başlamayı bekleyen işlerde kullanılacak toplam plaka sayısını tutar
        public int WaitingPlateQuantity { get; set; }

        // Şu anda atölyedeki işlerde kullanılan toplam plaka sayısını tutar
        public int InWorkshopPlateQuantity { get; set; }

        // Şu anda atölyedeki işlerin toplam kesim metresini tutar
        public decimal InWorkshopCuttingMeter { get; set; }

        // Kritik stok seviyesinde bulunan aktif taş sayısını tutar
        public int CriticalStockCount { get; set; }

        // Sistemde bulunan toplam plaka sayısını tutar
        public decimal TotalStock { get; set; }

        // Sistemde bulunan aktif taş çeşidi sayısını tutar
        public int ActiveStoneCount { get; set; }

        // Atölyeye başlamayı bekleyen iş emirlerini tutar
        public List<WorkshopWaitingWorkOrderViewModel> WaitingWorkOrders { get; set; }
            = new();

        // Şu anda atölyede işlem gören iş emirlerini tutar
        public List<WorkshopActiveWorkOrderViewModel> ActiveWorkOrders { get; set; }
            = new();

        // Kritik stok seviyesinde bulunan taşları tutar
        public List<WorkshopCriticalStockViewModel> CriticalStocks { get; set; }
            = new();

        // Son tamamlanan iş emirlerini tutar
        public List<WorkshopCompletedWorkOrderViewModel> CompletedWorkOrders { get; set; }
            = new();
    }


    public class WorkshopWaitingWorkOrderViewModel
    {
        // İş emrinin benzersiz kimliğini tutar
        public int WorkOrderId { get; set; }

        // Kullanıcıya gösterilecek iş emri numarasını tutar
        public string WorkOrderNumber { get; set; } = string.Empty;

        // İş emrinin ait olduğu müşterinin adını tutar
        public string CustomerName { get; set; } = string.Empty;

        // İş emrinin oluşturulma tarihini tutar
        public DateTime CreatedDate { get; set; }

        // İş emrinin toplam kesim metresini tutar
        public decimal CuttingMeter { get; set; }

        // İş emrinde kullanılacak toplam plaka sayısını tutar
        public int TotalPlateQuantity { get; set; }

        // İş emriyle ilgili atölyeye iletilen açıklamayı tutar
        public string? Description { get; set; }

        // İş emrinde kullanılacak taşların bilgilerini tutar
        public List<WorkshopStoneViewModel> Stones { get; set; }
            = new();
    }


    public class WorkshopActiveWorkOrderViewModel
    {
        // İş emrinin benzersiz kimliğini tutar
        public int WorkOrderId { get; set; }

        // Kullanıcıya gösterilecek iş emri numarasını tutar
        public string WorkOrderNumber { get; set; } = string.Empty;

        // İş emrinin ait olduğu müşterinin adını tutar
        public string CustomerName { get; set; } = string.Empty;

        // İş emrinin oluşturulma tarihini tutar
        public DateTime CreatedDate { get; set; }

        // İş emrinin toplam kesim metresini tutar
        public decimal CuttingMeter { get; set; }

        // İş emrinde kullanılan toplam plaka sayısını tutar
        public int TotalPlateQuantity { get; set; }

        // İş emriyle ilgili atölyeye iletilen açıklamayı tutar
        public string? Description { get; set; }

        // İş emrinde kullanılan taşların bilgilerini tutar
        public List<WorkshopStoneViewModel> Stones { get; set; }
            = new();
    }


    public class WorkshopStoneViewModel
    {
        // Taşın benzersiz kimliğini tutar
        public int StoneId { get; set; }

        // Taşın adını tutar
        public string StoneName { get; set; } = string.Empty;

        // Taşın renk bilgisini tutar
        public string? Color { get; set; }

        // Atölyecinin taşı görsel olarak tanıyabilmesi için taş fotoğrafını tutar
        public string? ImageUrl { get; set; }

        // Bu iş emrinde kullanılacak plaka sayısını tutar
        public int Quantity { get; set; }
    }


    public class WorkshopCriticalStockViewModel
    {
        // Taşın benzersiz kimliğini tutar
        public int StoneId { get; set; }

        // Taşın adını tutar
        public string StoneName { get; set; } = string.Empty;

        // Taşın renk bilgisini tutar
        public string? Color { get; set; }

        // Atölyecinin taşı görsel olarak tanıyabilmesi için taş fotoğrafını tutar
        public string? ImageUrl { get; set; }

        // Taşın mevcut plaka sayısını tutar
        public int CurrentStock { get; set; }
    }


    public class WorkshopCompletedWorkOrderViewModel
    {
        // İş emrinin benzersiz kimliğini tutar
        public int WorkOrderId { get; set; }

        // Kullanıcıya gösterilecek iş emri numarasını tutar
        public string WorkOrderNumber { get; set; } = string.Empty;

        // İş emrinin ait olduğu müşterinin adını tutar
        public string CustomerName { get; set; } = string.Empty;

        // İş emrinin oluşturulma tarihini tutar
        public DateTime CreatedDate { get; set; }

        // İş emrinde kullanılan toplam plaka sayısını tutar
        public int TotalPlateQuantity { get; set; }

        // İş emrinin toplam kesim metresini tutar
        public decimal CuttingMeter { get; set; }

        // Tamamlanan işte kullanılan taşların bilgilerini tutar
        public List<WorkshopStoneViewModel> Stones { get; set; }
            = new();
    }
}