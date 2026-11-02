using System.ComponentModel.DataAnnotations;
using StoneTech.Enums;

namespace StoneTech.Models.User
{
    public class UserCreateViewModel
    {
        // Kullanıcı hesabının bağlanacağı personelin Id bilgisi
        [Required(ErrorMessage = "Personel seçimi zorunludur.")]
        [Display(Name = "Personel")]
        public int EmployeeId { get; set; }

        // Sisteme girişte kullanılacak kullanıcı adı
        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        [StringLength( 50,MinimumLength = 3, ErrorMessage = "Kullanıcı adı 3 ile 50 karakter arasında olmalıdır.")]
        [Display(Name = "Kullanıcı Adı")]
        public string Username { get; set; } = string.Empty;

        // Kullanıcının sisteme giriş yaparken kullanacağı şifre
        [Required(ErrorMessage = "Şifre zorunludur.")]
        [StringLength(100,MinimumLength = 8,ErrorMessage = "Şifre en az 8 karakter olmalıdır.")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Password { get; set; } = string.Empty;

        // Girilen şifrenin tekrar doğrulanması
        [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password),ErrorMessage = "Şifreler birbiriyle eşleşmiyor.")]
        [Display(Name = "Şifre Tekrar")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // Kullanıcının sistemde sahip olacağı yetki rolü
        [Required(ErrorMessage = "Kullanıcı rolü seçilmelidir.")]
        [Display(Name = "Rol")]
        public UserRole Role { get; set; } = UserRole.Workshop;

        // Kullanıcı hesabı aktif olarak oluşturulsun mu?
        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;
    }
}