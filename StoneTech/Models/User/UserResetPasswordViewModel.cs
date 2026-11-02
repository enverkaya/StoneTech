using System.ComponentModel.DataAnnotations;

namespace StoneTech.Models.User
{
    public class UserResetPasswordViewModel
    {
        // Şifresi sıfırlanacak kullanıcının benzersiz Id bilgisi
        public int UserId { get; set; }

        // Kullanıcının bağlı olduğu personelin adı ve soyadı
        public string FullName { get; set; } = string.Empty;

        // Sisteme girişte kullanılan kullanıcı adı
        public string Username { get; set; } = string.Empty;

        // Admin tarafından belirlenecek yeni şifre
        [Required(ErrorMessage = "Yeni şifre zorunludur.")]
        [StringLength(
            100,
            MinimumLength = 8,
            ErrorMessage = "Şifre en az 8 karakter olmalıdır.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        // Yeni şifrenin doğrulanması için tekrar girilen değer
        [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
        [DataType(DataType.Password)]
        [Compare(
            nameof(NewPassword),
            ErrorMessage = "Girilen şifreler birbiriyle eşleşmiyor.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}