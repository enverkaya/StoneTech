using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace StoneTech.Models.Employee
{
    public class EmployeeEditViewModel
    {
        // Düzenlenecek personelin benzersiz Id bilgisi
        public int EmployeeId { get; set; }

        // Personelin adı ve soyadı
        [Required(ErrorMessage = "Ad soyad zorunludur.")]
        [StringLength(150, ErrorMessage = "Ad soyad en fazla 150 karakter olabilir.")]
        [Display(Name = "Ad Soyad")]
        public string FullName { get; set; } = string.Empty;

        // Personelin telefon numarası
        [StringLength(20, ErrorMessage = "Telefon numarası en fazla 20 karakter olabilir.")]
        [Display(Name = "Telefon")]
        public string? Phone { get; set; }

        // Personelin e-posta adresi
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [StringLength(150, ErrorMessage = "E-posta adresi en fazla 150 karakter olabilir.")]
        [Display(Name = "E-posta")]
        public string? Email { get; set; }

        // Personelin işletmedeki görevi
        [Required(ErrorMessage = "Görev zorunludur.")]
        [StringLength(100, ErrorMessage = "Görev en fazla 100 karakter olabilir.")]
        [Display(Name = "Görev")]
        public string Position { get; set; } = string.Empty;

        // Personelin işe giriş tarihi
        [Required(ErrorMessage = "İşe giriş tarihi zorunludur.")]
        [DataType(DataType.Date)]
        [Display(Name = "İşe Giriş Tarihi")]
        public DateTime HireDate { get; set; }

        // Personelin işten ayrılma tarihi
        [DataType(DataType.Date)]
        [Display(Name = "İşten Ayrılma Tarihi")]
        public DateTime? TerminationDate { get; set; }

        // Personelle ilgili açıklama veya not
        [StringLength(500, ErrorMessage = "Not en fazla 500 karakter olabilir.")]
        [Display(Name = "Not")]
        public string? Note { get; set; }

        // Personelin veritabanında kayıtlı mevcut profil fotoğrafının yolu
        public string? CurrentImageUrl { get; set; }

        // Personel için yüklenecek yeni profil fotoğrafı
        [Display(Name = "Yeni Profil Fotoğrafı")]
        public IFormFile? ImageFile { get; set; }

        // Personel aktif olarak çalışıyor mu?
        [Display(Name = "Aktif")]
        public bool IsActive { get; set; }
    }
}