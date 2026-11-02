using StoneTech.Enums;

namespace StoneTech.Models.Dashboard
{
    public class ManagerDashboardViewModel
    {
        // Dashboard'da karşılanacak giriş yapmış yöneticinin adını ve soyadını tutar
        public string CurrentUserFullName { get; set; } = string.Empty;

        // Bu ay tamamlanan işlerden oluşan toplam ciroyu tutar
        public decimal MonthlyRevenue { get; set; }

        // Bu ay alınan toplam tahsilatı tutar
        public decimal MonthlyCollection { get; set; }

        // Tamamlanan işlerde henüz tahsil edilmemiş toplam alacağı tutar
        public decimal TotalReceivable { get; set; }

        // Bu ay yapılan toplam satın alma tutarını tutar
        public decimal MonthlyPurchaseAmount { get; set; }

        // Yönetici onayı bekleyen iş emri sayısını tutar
        public int PendingApprovalCount { get; set; }

        // Onaylanmış ve atölyeye başlamayı bekleyen iş emri sayısını tutar
        public int ApprovedWorkOrderCount { get; set; }

        // Şu anda atölyede işlem gören iş emri sayısını tutar
        public int InWorkshopCount { get; set; }

        // Bu ay tamamlanan iş emri sayısını tutar
        public int MonthlyCompletedCount { get; set; }

        // Sistemde bulunan toplam plaka sayısını tutar
        public decimal TotalStock { get; set; }

        // Kritik stok seviyesinde bulunan aktif taş sayısını tutar
        public int CriticalStockCount { get; set; }

        // Bu ay oluşturulan fatura sayısını tutar
        public int MonthlyInvoiceCount { get; set; }

        // Sistemdeki aktif müşteri sayısını tutar
        public int ActiveCustomerCount { get; set; }

        // Dashboard'da gösterilecek onay bekleyen iş emirlerini tutar
        public List<ManagerPendingWorkOrderViewModel> PendingWorkOrders { get; set; }
            = new();

        // Dashboard'da gösterilecek atölyedeki iş emirlerini tutar
        public List<ManagerWorkshopWorkOrderViewModel> WorkshopWorkOrders { get; set; }
            = new();

        // Dashboard'da gösterilecek tahsilat bekleyen işleri tutar
        public List<ManagerReceivableViewModel> Receivables { get; set; }
            = new();

        // Dashboard'da gösterilecek kritik stokları tutar
        public List<ManagerCriticalStockViewModel> CriticalStocks { get; set; }
            = new();
    }


    public class ManagerPendingWorkOrderViewModel
    {
        // İş emrinin benzersiz kimliğini tutar
        public int WorkOrderId { get; set; }

        // Kullanıcıya gösterilecek iş emri numarasını tutar
        public string WorkOrderNumber { get; set; } = string.Empty;

        // İş emrinin ait olduğu müşterinin adını tutar
        public string CustomerName { get; set; } = string.Empty;

        // İş emrinin oluşturulma tarihini tutar
        public DateTime CreatedDate { get; set; }

        // İş emrinin genel toplam tutarını tutar
        public decimal TotalAmount { get; set; }

        // İş emrinin mevcut durumunu tutar
        public WorkOrderStatus Status { get; set; }
    }


    public class ManagerWorkshopWorkOrderViewModel
    {
        // İş emrinin benzersiz kimliğini tutar
        public int WorkOrderId { get; set; }

        // Kullanıcıya gösterilecek iş emri numarasını tutar
        public string WorkOrderNumber { get; set; } = string.Empty;

        // İş emrinin ait olduğu müşterinin adını tutar
        public string CustomerName { get; set; } = string.Empty;

        // İş emrinde kullanılan toplam plaka sayısını tutar
        public int TotalPlateQuantity { get; set; }

        // İş emrindeki toplam kesim metresini tutar
        public decimal CuttingMeter { get; set; }

        // İş emrinin oluşturulma tarihini tutar
        public DateTime CreatedDate { get; set; }

        // İş emrinin mevcut durumunu tutar
        public WorkOrderStatus Status { get; set; }
    }


    public class ManagerReceivableViewModel
    {
        // İş emrinin benzersiz kimliğini tutar
        public int WorkOrderId { get; set; }

        // Kullanıcıya gösterilecek iş emri numarasını tutar
        public string WorkOrderNumber { get; set; } = string.Empty;

        // Borcun ait olduğu müşterinin adını tutar
        public string CustomerName { get; set; } = string.Empty;

        // İş emrinin müşteriye ait genel toplamını tutar
        public decimal TotalAmount { get; set; }

        // İş emrine bugüne kadar yapılan toplam tahsilatı tutar
        public decimal PaidAmount { get; set; }

        // Müşteriden tahsil edilmesi gereken kalan tutarı tutar
        public decimal RemainingAmount { get; set; }
    }


    public class ManagerCriticalStockViewModel
    {
        // Taşın benzersiz kimliğini tutar
        public int StoneId { get; set; }

        // Taşın adını tutar
        public string StoneName { get; set; } = string.Empty;

        // Taşın rengini tutar
        public string? Color { get; set; }

        // Taşın mevcut plaka sayısını tutar
        public int CurrentStock { get; set; }
    }
}