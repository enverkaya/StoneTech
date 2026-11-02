namespace StoneTech.Models.Invoice
{
    public class InvoiceCompanyViewModel
    {
        // Faturayı düzenleyen işletmenin resmi unvanını tutar
        public string CompanyName { get; set; } = string.Empty;

        // İşletmenin bağlı olduğu vergi dairesini tutar
        public string TaxOffice { get; set; } = string.Empty;

        // İşletmenin vergi numarasını tutar
        public string TaxNumber { get; set; } = string.Empty;

        // İşletmenin açık adresini tutar
        public string Address { get; set; } = string.Empty;

        // İşletmenin telefon numarasını tutar
        public string? Phone { get; set; }

        // İşletmenin e-posta adresini tutar
        public string? Email { get; set; }

        // İşletmenin internet sitesi adresini tutar
        public string? Website { get; set; }

        // İşletmenin fatura üzerinde kullanılacak logo yolunu tutar
        public string? LogoUrl { get; set; }
    }
}