using StoneTech.Enums;

namespace StoneTech.Models.Dashboard
{
    public class SalesDashboardViewModel
    {
        // Dashboard'da karşılanacak giriş yapmış satış personelinin adını ve soyadını tutar
        public string CurrentUserFullName { get; set; } = string.Empty;

        // Sistemdeki aktif müşteri sayısını tutar
        public int ActiveCustomerCount { get; set; }

        // Bu ay sisteme eklenen müşteri sayısını tutar
        public int MonthlyNewCustomerCount { get; set; }

        // Yönetici onayı bekleyen iş emri sayısını tutar
        public int PendingApprovalCount { get; set; }

        // Yönetici tarafından onaylanmış iş emri sayısını tutar
        public int ApprovedWorkOrderCount { get; set; }

        // Şu anda atölyede bulunan iş emri sayısını tutar
        public int InWorkshopCount { get; set; }

        // Bu ay tamamlanan iş emri sayısını tutar
        public int MonthlyCompletedCount { get; set; }

        // Bu ay oluşturulan toplam iş emri sayısını tutar
        public int MonthlyWorkOrderCount { get; set; }

        // Bu ay oluşturulan iş emirlerinin toplam tutarını tutar
        public decimal MonthlyWorkOrderAmount { get; set; }

        // Bu ay tamamlanan işlerden oluşan toplam ciroyu tutar
        public decimal MonthlyRevenue { get; set; }

        // Tamamlanan işlerde henüz tahsil edilmemiş toplam alacağı tutar
        public decimal TotalReceivable { get; set; }

        // Bu ay oluşturulan fatura sayısını tutar
        public int MonthlyInvoiceCount { get; set; }

        // Sistemde bulunan toplam plaka sayısını tutar
        public decimal TotalStock { get; set; }

        // Dashboard'da gösterilecek son iş emirlerini tutar
        public List<SalesDashboardWorkOrderViewModel> RecentWorkOrders { get; set; }
            = new();

        // Dashboard'da gösterilecek son müşterileri tutar
        public List<SalesDashboardCustomerViewModel> RecentCustomers { get; set; }
            = new();

        // Dashboard'da gösterilecek onay bekleyen iş emirlerini tutar
        public List<SalesDashboardPendingWorkOrderViewModel> PendingWorkOrders { get; set; }
            = new();

        // Dashboard'da gösterilecek tamamlanan son işleri tutar
        public List<SalesDashboardCompletedWorkOrderViewModel> CompletedWorkOrders { get; set; }
            = new();
    }


    public class SalesDashboardWorkOrderViewModel
    {
        // İş emrinin benzersiz kimliğini tutar
        public int WorkOrderId { get; set; }

        // Kullanıcıya gösterilecek iş emri numarasını tutar
        public string WorkOrderNumber { get; set; } = string.Empty;

        // İş emrinin ait olduğu müşterinin adını tutar
        public string CustomerName { get; set; } = string.Empty;

        // İş emrinin oluşturulma tarihini tutar
        public DateTime CreatedDate { get; set; }

        // İş emrinin toplam tutarını tutar
        public decimal TotalAmount { get; set; }

        // İş emrinin mevcut durumunu tutar
        public WorkOrderStatus Status { get; set; }
    }


    public class SalesDashboardCustomerViewModel
    {
        // Müşterinin benzersiz kimliğini tutar
        public int CustomerId { get; set; }

        // Müşterinin adını veya firma unvanını tutar
        public string CustomerName { get; set; } = string.Empty;

        // Müşterinin bireysel veya kurumsal türünü tutar
        public CustomerType CustomerType { get; set; }

        // Müşterinin telefon numarasını tutar
        public string Phone { get; set; } = string.Empty;

        // Müşterinin e-posta adresini tutar
        public string? Email { get; set; }

        // Müşterinin aktif olup olmadığını tutar
        public bool IsActive { get; set; }
    }


    public class SalesDashboardPendingWorkOrderViewModel
    {
        // İş emrinin benzersiz kimliğini tutar
        public int WorkOrderId { get; set; }

        // Kullanıcıya gösterilecek iş emri numarasını tutar
        public string WorkOrderNumber { get; set; } = string.Empty;

        // İş emrinin ait olduğu müşterinin adını tutar
        public string CustomerName { get; set; } = string.Empty;

        // İş emrinin oluşturulma tarihini tutar
        public DateTime CreatedDate { get; set; }

        // İş emrinin toplam tutarını tutar
        public decimal TotalAmount { get; set; }
    }


    public class SalesDashboardCompletedWorkOrderViewModel
    {
        // İş emrinin benzersiz kimliğini tutar
        public int WorkOrderId { get; set; }

        // Kullanıcıya gösterilecek iş emri numarasını tutar
        public string WorkOrderNumber { get; set; } = string.Empty;

        // İş emrinin ait olduğu müşterinin adını tutar
        public string CustomerName { get; set; } = string.Empty;

        // İş emrinin toplam tutarını tutar
        public decimal TotalAmount { get; set; }

        // İş emrinin mevcut durumunu tutar
        public WorkOrderStatus Status { get; set; }
    }
}