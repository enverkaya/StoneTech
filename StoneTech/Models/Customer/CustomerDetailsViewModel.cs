using StoneTech.Enums;

namespace StoneTech.Models.Customer
{
    public class CustomerDetailsViewModel
    {
        // Müşterinin Id'si
        public int CustomerId { get; set; }

        // Müşteri tipi
        public CustomerType CustomerType { get; set; }

        // Müşterinin adı soyadı veya firma unvanı
        public string CustomerName { get; set; } = string.Empty;

        // Bireysel müşterinin T.C. kimlik numarası
        public string? IdentityNumber { get; set; }

        // Kurumsal müşterinin vergi kimlik numarası
        public string? TaxNumber { get; set; }

        // Kurumsal müşterinin bağlı olduğu vergi dairesi
        public string? TaxOffice { get; set; }

        // Müşterinin telefon numarası
        public string Phone { get; set; } = string.Empty;

        // Müşterinin e-posta adresi
        public string? Email { get; set; }

        // Müşterinin adres bilgisi
        public string? Address { get; set; }

        // Müşteriyle ilgili özel not veya açıklama
        public string? Note { get; set; }

        // Müşterinin sistemde aktif olup olmadığını belirtir
        public bool IsActive { get; set; }

        // Müşteriye ait toplam iş emri sayısı
        public int WorkOrderCount { get; set; }

        // Müşteriye ait iş emirlerinin toplam tutarı
        public decimal TotalAmount { get; set; }

        // Müşterinin yaptığı toplam ödeme
        public decimal TotalPaid { get; set; }

        // Müşterinin kalan toplam borcu
        public decimal RemainingDebt { get; set; }

        // Müşteriye ait iş emirlerinin listesi
        public List<CustomerWorkOrderViewModel> WorkOrders { get; set; }
            = new List<CustomerWorkOrderViewModel>();
    }

    public class CustomerWorkOrderViewModel
    {
        // İş emrinin veritabanı kimliği
        // Yönlendirme işlemlerinde kullanılır, kullanıcıya gösterilmez
        public int WorkOrderId { get; set; }

        // Kullanıcıya gösterilecek iş emri numarası
        // Örnek: IE-2026-000001
        public string WorkOrderNumber { get; set; } = string.Empty;

        // İş emrinin oluşturulma tarihi
        public DateTime CreatedDate { get; set; }

        // İş emrinin KDV dahil toplam tutarı
        public decimal TotalAmount { get; set; }

        // İş emrine yapılan toplam ödeme
        public decimal TotalPaid { get; set; }

        // İş emrinin kalan borcu
        public decimal RemainingAmount { get; set; }

        // İş emrinin mevcut durumu
        public WorkOrderStatus Status { get; set; }

        // İş emriyle ilgili açıklama
        public string? Description { get; set; }
    }
}