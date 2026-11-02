using StoneTech.Enums;

namespace StoneTech.Models.Dashboard
{
    public class AccountantDashboardViewModel
    {
        // Dashboard'da karşılanacak giriş yapmış muhasebe personelinin adını ve soyadını tutar
        public string CurrentUserFullName { get; set; } = string.Empty;

        // Bu ay tamamlanan işlerden oluşan toplam ciroyu tutar
        public decimal MonthlyRevenue { get; set; }

        // Bu ay alınan toplam tahsilatı tutar
        public decimal MonthlyCollection { get; set; }

        // Tamamlanan işlerde henüz tahsil edilmemiş toplam alacağı tutar
        public decimal TotalReceivable { get; set; }

        // Bu ay yapılan toplam satın alma tutarını tutar
        public decimal MonthlyPurchaseAmount { get; set; }

        // Bu ay oluşturulan toplam fatura sayısını tutar
        public int MonthlyInvoiceCount { get; set; }

        // Sistemde bulunan taslak fatura sayısını tutar
        public int DraftInvoiceCount { get; set; }

        // Sistemde bulunan kesilmiş fatura sayısını tutar
        public int IssuedInvoiceCount { get; set; }

        // Sistemde bulunan iptal edilmiş fatura sayısını tutar
        public int CancelledInvoiceCount { get; set; }

        // Tamamlanmış fakat henüz faturası oluşturulmamış iş emri sayısını tutar
        public int UninvoicedCompletedWorkOrderCount { get; set; }

        // Tamamlanmış fakat ödemesi tamamen alınmamış iş emri sayısını tutar
        public int ReceivableWorkOrderCount { get; set; }

        // Bu ay yapılan tahsilat sayısını tutar
        public int MonthlyPaymentCount { get; set; }

        // Sistemdeki aktif tedarikçi sayısını tutar
        public int ActiveSupplierCount { get; set; }

        // Dashboard'da gösterilecek son tahsilatları tutar
        public List<AccountantDashboardPaymentViewModel> RecentPayments { get; set; }
            = new();

        // Dashboard'da gösterilecek tahsilat bekleyen işleri tutar
        public List<AccountantDashboardReceivableViewModel> Receivables { get; set; }
            = new();

        // Dashboard'da gösterilecek son faturaları tutar
        public List<AccountantDashboardInvoiceViewModel> RecentInvoices { get; set; }
            = new();

        // Dashboard'da gösterilecek son satın almaları tutar
        public List<AccountantDashboardPurchaseViewModel> RecentPurchases { get; set; }
            = new();
    }


    public class AccountantDashboardPaymentViewModel
    {
        // Ödemenin benzersiz kimliğini tutar
        public int PaymentId { get; set; }

        // Ödemenin bağlı olduğu iş emrinin kimliğini tutar
        public int WorkOrderId { get; set; }

        // Kullanıcıya gösterilecek iş emri numarasını tutar
        public string WorkOrderNumber { get; set; } = string.Empty;

        // Ödemenin ait olduğu müşterinin adını tutar
        public string CustomerName { get; set; } = string.Empty;

        // Alınan ödeme tutarını tutar
        public decimal Amount { get; set; }

        // Ödemenin hangi yöntemle alındığını tutar
        public PaymentMethod PaymentMethod { get; set; }

        // Ödemenin alındığı tarihi tutar
        public DateTime PaymentDate { get; set; }
    }


    public class AccountantDashboardReceivableViewModel
    {
        // İş emrinin benzersiz kimliğini tutar
        public int WorkOrderId { get; set; }

        // Kullanıcıya gösterilecek iş emri numarasını tutar
        public string WorkOrderNumber { get; set; } = string.Empty;

        // Borcu bulunan müşterinin adını tutar
        public string CustomerName { get; set; } = string.Empty;

        // İş emrinin toplam tutarını tutar
        public decimal TotalAmount { get; set; }

        // İş emrine bugüne kadar yapılan toplam tahsilatı tutar
        public decimal PaidAmount { get; set; }

        // Müşteriden tahsil edilmesi gereken kalan tutarı tutar
        public decimal RemainingAmount { get; set; }
    }


    public class AccountantDashboardInvoiceViewModel
    {
        // Faturanın benzersiz kimliğini tutar
        public int InvoiceId { get; set; }

        // Kullanıcıya gösterilecek fatura numarasını tutar
        public string InvoiceNumber { get; set; } = string.Empty;

        // Faturanın bağlı olduğu iş emrinin kimliğini tutar
        public int WorkOrderId { get; set; }

        // Faturanın bağlı olduğu iş emri numarasını tutar
        public string WorkOrderNumber { get; set; } = string.Empty;

        // Fatura üzerindeki müşteri adını tutar
        public string CustomerName { get; set; } = string.Empty;

        // Faturanın oluşturulma tarihini tutar
        public DateTime InvoiceDate { get; set; }

        // Faturanın toplam tutarını tutar
        public decimal TotalAmount { get; set; }

        // Faturanın mevcut durumunu tutar
        public InvoiceStatus Status { get; set; }
    }


    public class AccountantDashboardPurchaseViewModel
    {
        // Satın almanın benzersiz kimliğini tutar
        public int PurchaseId { get; set; }

        // Kullanıcıya gösterilecek satın alma numarasını tutar
        public string PurchaseNumber { get; set; } = string.Empty;

        // Satın almanın yapıldığı tedarikçinin adını tutar
        public string SupplierName { get; set; } = string.Empty;

        // Satın alma tarihini tutar
        public DateTime PurchaseDate { get; set; }

        // Satın almanın toplam tutarını tutar
        public decimal TotalAmount { get; set; }
    }
}