using System.ComponentModel.DataAnnotations;
using StoneTech.Enums;

namespace StoneTech.Models.Customer
{
    public class CustomerCreateViewModel
    {
        // Müşteri tipi: Bireysel veya Kurumsal
        [Required(ErrorMessage = "Müşteri tipi seçilmelidir.")]
        public CustomerType CustomerType { get; set; } = CustomerType.Individual;

        // Bireysel müşteride ad soyad, kurumsal müşteride firma unvanı
        [Required(ErrorMessage = "Müşteri adı veya firma unvanı zorunludur.")]
        [StringLength(150, ErrorMessage = "Müşteri adı en fazla 150 karakter olabilir.")]
        public string CustomerName { get; set; } = string.Empty;

        // Bireysel müşterinin T.C. kimlik numarası
        [StringLength(11, MinimumLength = 11,
            ErrorMessage = "T.C. kimlik numarası 11 haneli olmalıdır.")]
        [RegularExpression(@"^\d{11}$",
            ErrorMessage = "T.C. kimlik numarası yalnızca rakamlardan oluşmalıdır.")]
        public string? IdentityNumber { get; set; }

        // Kurumsal müşterinin vergi kimlik numarası
        [StringLength(10, MinimumLength = 10,
            ErrorMessage = "Vergi numarası 10 haneli olmalıdır.")]
        [RegularExpression(@"^\d{10}$",
            ErrorMessage = "Vergi numarası yalnızca rakamlardan oluşmalıdır.")]
        public string? TaxNumber { get; set; }

        // Kurumsal müşterinin bağlı olduğu vergi dairesi
        [StringLength(100,
            ErrorMessage = "Vergi dairesi en fazla 100 karakter olabilir.")]
        public string? TaxOffice { get; set; }

        // Müşterinin telefon numarası
        [Required(ErrorMessage = "Telefon numarası zorunludur.")]
        [StringLength(20,
            ErrorMessage = "Telefon numarası en fazla 20 karakter olabilir.")]
        public string Phone { get; set; } = string.Empty;

        // Müşterinin e-posta adresi
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [StringLength(150,
            ErrorMessage = "E-posta adresi en fazla 150 karakter olabilir.")]
        public string? Email { get; set; }

        // Müşterinin fatura ve iletişim adresi
        [StringLength(500,
            ErrorMessage = "Adres en fazla 500 karakter olabilir.")]
        public string? Address { get; set; }

        // Müşteriyle ilgili özel not veya açıklama
        [StringLength(500,
            ErrorMessage = "Not en fazla 500 karakter olabilir.")]
        public string? Note { get; set; }
    }
}