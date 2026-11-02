using StoneTech.Enums;

namespace StoneTech.Models.Dashboard
{
    public class AdminDashboardViewModel
    {
        // Dashboard'da karşılanacak giriş yapmış kullanıcının adını tutar
        public string CurrentUserFullName { get; set; } = string.Empty;

        // Bu ay tamamlanan işlerden oluşan toplam ciroyu tutar
        public decimal MonthlyRevenue { get; set; }

        // Bu ay alınan toplam tahsilatı tutar
        public decimal MonthlyCollection { get; set; }

        // Tamamlanan işlerde henüz tahsil edilmemiş toplam alacağı tutar
        public decimal TotalReceivable { get; set; }

        // Sistemde bulunan toplam plaka sayısını tutar
        public decimal TotalStock { get; set; }

        // Yönetici onayı bekleyen iş emri sayısını tutar
        public int PendingApprovalCount { get; set; }

        // Atölyede işlem gören iş emri sayısını tutar
        public int InWorkshopCount { get; set; }

        // Bu ay tamamlanan iş emri sayısını tutar
        public int MonthlyCompletedCount { get; set; }

        // Stok seviyesi düşük olan taş sayısını tutar
        public int CriticalStockCount { get; set; }

        // Bu ay yapılan toplam satın alma tutarını tutar
        public decimal MonthlyPurchaseAmount { get; set; }

        // Bu ay oluşturulan fatura sayısını tutar
        public int MonthlyInvoiceCount { get; set; }

        // Aktif müşteri sayısını tutar
        public int ActiveCustomerCount { get; set; }

        // Aktif tedarikçi sayısını tutar
        public int ActiveSupplierCount { get; set; }

        // Dashboard'da gösterilecek son iş emirlerini tutar
        public List<AdminDashboardWorkOrderViewModel> RecentWorkOrders { get; set; }
            = new();

        // Dashboard'da gösterilecek son tahsilatları tutar
        public List<AdminDashboardPaymentViewModel> RecentPayments { get; set; }
            = new();

        // Dashboard'da gösterilecek son kullanıcı hesaplarını tutar
        public List<AdminDashboardUserViewModel> RecentUsers { get; set; }
            = new();

        // Dashboard'da gösterilecek son personelleri tutar
        public List<AdminDashboardEmployeeViewModel> RecentEmployees { get; set; }
            = new();
    }


    public class AdminDashboardWorkOrderViewModel
    {
        // İş emrinin benzersiz kimliğini tutar
        public int WorkOrderId { get; set; }

        // Kullanıcıya gösterilecek iş emri numarasını tutar
        public string WorkOrderNumber { get; set; } = string.Empty;

        // İş emrinin ait olduğu müşterinin adını tutar
        public string CustomerName { get; set; } = string.Empty;

        // İş emrinin genel toplam tutarını tutar
        public decimal TotalAmount { get; set; }

        // İş emrinin mevcut durumunu tutar
        public WorkOrderStatus Status { get; set; }

        // İş emrinin oluşturulma tarihini tutar
        public DateTime CreatedDate { get; set; }
    }


    public class AdminDashboardPaymentViewModel
    {
        // Tahsilat kaydının benzersiz kimliğini tutar
        public int PaymentId { get; set; }

        // Tahsilatın bağlı olduğu iş emrinin kimliğini tutar
        public int WorkOrderId { get; set; }

        // Kullanıcıya gösterilecek iş emri numarasını tutar
        public string WorkOrderNumber { get; set; } = string.Empty;

        // Ödeme yapan müşterinin adını tutar
        public string CustomerName { get; set; } = string.Empty;

        // Tahsil edilen tutarı tutar
        public decimal Amount { get; set; }

        // Tahsilat yöntemini tutar
        public PaymentMethod PaymentMethod { get; set; }

        // Tahsilat tarihini tutar
        public DateTime PaymentDate { get; set; }
    }


    public class AdminDashboardUserViewModel
    {
        // Kullanıcı hesabının benzersiz kimliğini tutar
        public int UserId { get; set; }

        // Kullanıcı hesabının bağlı olduğu personelin adını tutar
        public string EmployeeFullName { get; set; } = string.Empty;

        // Sisteme girişte kullanılan kullanıcı adını tutar
        public string Username { get; set; } = string.Empty;

        // Kullanıcının sistemdeki rolünü tutar
        public UserRole Role { get; set; }

        // Kullanıcının sisteme yaptığı son başarılı giriş tarihini tutar
        public DateTime? LastLoginDate { get; set; }

        // Kullanıcı hesabının aktif olup olmadığını tutar
        public bool IsActive { get; set; }
    }


    public class AdminDashboardEmployeeViewModel
    {
        // Personelin benzersiz kimliğini tutar
        public int EmployeeId { get; set; }

        // Personelin adını ve soyadını tutar
        public string FullName { get; set; } = string.Empty;

        // Personelin işletmedeki görevini tutar
        public string Position { get; set; } = string.Empty;

        // Personelin işe giriş tarihini tutar
        public DateTime HireDate { get; set; }

        // Personelin aktif olarak çalışıp çalışmadığını tutar
        public bool IsActive { get; set; }

        // Personelin profil fotoğrafının yolunu tutar
        public string? ImageUrl { get; set; }
    }
}