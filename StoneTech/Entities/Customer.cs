using StoneTech.Enums;

namespace StoneTech.Entities
{
    public class Customer
    {
        // Müşterinin benzersiz kimliği
        public int CustomerId { get; set; }

        // Müşteri tipi: Bireysel veya Kurumsal
        public CustomerType CustomerType { get; set; } = CustomerType.Individual;

        // Bireysel müşteride ad soyad, kurumsal müşteride firma unvanı
        public string CustomerName { get; set; } = string.Empty;

        // Bireysel müşteri için T.C. kimlik numarası
        public string? IdentityNumber { get; set; }

        // Kurumsal müşteri için vergi kimlik numarası
        public string? TaxNumber { get; set; }

        // Kurumsal müşteri için vergi dairesi
        public string? TaxOffice { get; set; }

        // Telefon numarası
        public string Phone { get; set; } = string.Empty;

        // E-posta adresi
        public string? Email { get; set; }

        // Fatura ve iletişim adresi
        public string? Address { get; set; }

        // Müşteriyle ilgili not
        public string? Note { get; set; }

        // Müşteri aktif mi?
        public bool IsActive { get; set; } = true;

        // Müşteriye ait iş emirleri
        public ICollection<WorkOrder> WorkOrders { get; set; }
            = new List<WorkOrder>();
    }
}