using System.ComponentModel.DataAnnotations;

namespace StoneTech.Models.Account
{
    public class LoginViewModel
    {
        // Sisteme girişte kullanılacak kullanıcı adı
        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        [StringLength(
            50,
            ErrorMessage = "Kullanıcı adı en fazla 50 karakter olabilir.")]
        [Display(Name = "Kullanıcı Adı")]
        public string Username { get; set; } = string.Empty;

        // Kullanıcının sisteme giriş şifresi
        [Required(ErrorMessage = "Şifre zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Password { get; set; } = string.Empty;

        // Kullanıcının oturumunun tarayıcı kapatıldıktan sonra da devam edip etmeyeceğini belirler
        [Display(Name = "Beni Hatırla")]
        public bool RememberMe { get; set; }
    }
}