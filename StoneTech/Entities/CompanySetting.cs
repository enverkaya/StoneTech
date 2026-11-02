
namespace StoneTech.Entities
{
    public class CompanySetting
    {
        // Firma ayar kaydının benzersiz kimliği
        public int CompanySettingId { get; set; }

        // Faturalarda gösterilecek resmi firma unvanı
        public string CompanyName { get; set; } = string.Empty;

        // Firmanın vergi dairesi
        public string TaxOffice { get; set; } = string.Empty;

        // Firmanın vergi numarası
        public string TaxNumber { get; set; } = string.Empty;

        // Firmanın açık adresi
        public string Address { get; set; } = string.Empty;

        // Firmanın telefon numarası
        public string? Phone { get; set; }

        // Firmanın e-posta adresi
        public string? Email { get; set; }

        // Firmanın internet sitesi adresi
        public string? Website { get; set; }

        // Firma logosunun dosya yolu
        public string? LogoUrl { get; set; }

        // Firmanın ikinci logosunun dosya yolu
        public string? SecondLogoUrl { get; set; }

        // Firmanın Instagram profil bağlantısı
        public string? InstagramUrl { get; set; }

        // Firmanın Facebook sayfası bağlantısı
        public string? FacebookUrl { get; set; }

        // Firmanın WhatsApp iletişim bağlantısı
        public string? WhatsAppUrl { get; set; }
    }
}
