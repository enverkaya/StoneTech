using System.ComponentModel.DataAnnotations;
using StoneTech.Enums;

namespace StoneTech.Models.User
{
    public class UserEditViewModel
    {
        // Düzenlenecek kullanıcının benzersiz Id bilgisi
        public int UserId { get; set; }

        // Kullanıcı hesabının bağlı olduğu personelin Id bilgisi
        public int EmployeeId { get; set; }

        // Kullanıcı hesabının bağlı olduğu personelin adı ve soyadı
        public string FullName { get; set; } = string.Empty;

        // Sisteme girişte kullanılacak kullanıcı adı
        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        [StringLength(50,MinimumLength = 3,ErrorMessage = "Kullanıcı adı 3 ile 50 karakter arasında olmalıdır.")]
        [Display(Name = "Kullanıcı Adı")]
        public string Username { get; set; } = string.Empty;

        // Kullanıcının sistemde sahip olduğu yetki rolü
        [Required(ErrorMessage = "Kullanıcı rolü seçilmelidir.")]
        [Display(Name = "Rol")]
        public UserRole Role { get; set; }

        // Kullanıcı hesabı aktif mi?
        [Display(Name = "Aktif")]
        public bool IsActive { get; set; }
    }
}