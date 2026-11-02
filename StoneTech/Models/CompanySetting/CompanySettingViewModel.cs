
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace StoneTech.Models.CompanySetting
{
    public class CompanySettingViewModel
    {
        // Firma ayar kaydının benzersiz kimliği
        public int CompanySettingId { get; set; }

        // Faturalarda ve kurumsal belgelerde gösterilecek resmi firma unvanı
        [Required(ErrorMessage = "Firma unvanı zorunludur.")]
        [StringLength(200, ErrorMessage = "Firma unvanı en fazla 200 karakter olabilir.")]
        public string CompanyName { get; set; } = string.Empty;

        // Firmanın bağlı olduğu vergi dairesi
        [Required(ErrorMessage = "Vergi dairesi zorunludur.")]
        [StringLength(100, ErrorMessage = "Vergi dairesi en fazla 100 karakter olabilir.")]
        public string TaxOffice { get; set; } = string.Empty;

        // Firmanın vergi numarası
        [Required(ErrorMessage = "Vergi numarası zorunludur.")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Vergi numarası 10 haneli olmalıdır.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Vergi numarası yalnızca 10 rakamdan oluşmalıdır.")]
        public string TaxNumber { get; set; } = string.Empty;

        // Firmanın faturalarda kullanılacak açık adresi
        [Required(ErrorMessage = "Firma adresi zorunludur.")]
        [StringLength(500, ErrorMessage = "Adres en fazla 500 karakter olabilir.")]
        public string Address { get; set; } = string.Empty;

        // Firmanın iletişim telefonu
        [StringLength(20, ErrorMessage = "Telefon numarası en fazla 20 karakter olabilir.")]
        public string? Phone { get; set; }

        // Firmanın iletişim e-posta adresi
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [StringLength(150, ErrorMessage = "E-posta adresi en fazla 150 karakter olabilir.")]
        public string? Email { get; set; }

        // Firmanın internet sitesi adresi
        [StringLength(200, ErrorMessage = "İnternet sitesi adresi en fazla 200 karakter olabilir.")]
        public string? Website { get; set; }

        // Veritabanında kayıtlı mevcut firma logosunun yolu
        public string? ExistingLogoUrl { get; set; }

        // Kullanıcının yükleyeceği yeni firma logosu
        public IFormFile? LogoFile { get; set; }

        // Veritabanında kayıtlı mevcut ikinci logonun yolu
        public string? ExistingSecondLogoUrl { get; set; }

        // Kullanıcının yükleyeceği yeni ikinci firma logosu
        public IFormFile? SecondLogoFile { get; set; }

        // Firmanın Instagram profil bağlantısı
        [StringLength(300, ErrorMessage = "Instagram bağlantısı en fazla 300 karakter olabilir.")]
        [Url(ErrorMessage = "Geçerli bir Instagram bağlantısı giriniz.")]
        public string? InstagramUrl { get; set; }

        // Firmanın Facebook sayfası bağlantısı
        [StringLength(300, ErrorMessage = "Facebook bağlantısı en fazla 300 karakter olabilir.")]
        [Url(ErrorMessage = "Geçerli bir Facebook bağlantısı giriniz.")]
        public string? FacebookUrl { get; set; }

        // Firmanın WhatsApp iletişim bağlantısı
        [StringLength(300, ErrorMessage = "WhatsApp bağlantısı en fazla 300 karakter olabilir.")]
        [Url(ErrorMessage = "Geçerli bir WhatsApp bağlantısı giriniz.")]
        public string? WhatsAppUrl { get; set; }
    }
}
