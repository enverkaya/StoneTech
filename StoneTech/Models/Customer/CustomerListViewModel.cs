using StoneTech.Enums;

namespace StoneTech.Models.Customer
{
    public class CustomerListViewModel
    {
        // Müşterinin Id'si
        public int CustomerId { get; set; }

        // Müşteri tipi
        public CustomerType CustomerType { get; set; }

        // Müşterinin adı soyadı veya firma unvanı
        public string CustomerName { get; set; } = string.Empty;

        // Müşterinin telefon numarası
        public string Phone { get; set; } = string.Empty;

        // Müşterinin e-posta adresi
        public string? Email { get; set; }

        // Müşterinin adres bilgisi
        public string? Address { get; set; }

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
    }
}