using StoneTech.Enums;

namespace StoneTech.Models.User
{
    public class UserDetailsViewModel
    {
        // Kullanıcının benzersiz Id bilgisi
        public int UserId { get; set; }

        // Kullanıcı hesabının bağlı olduğu personelin Id bilgisi
        public int EmployeeId { get; set; }

        // Personelin adı ve soyadı
        public string FullName { get; set; } = string.Empty;

        // Personelin işletmedeki görevi
        public string Position { get; set; } = string.Empty;

        // Personelin telefon numarası
        public string? Phone { get; set; }

        // Personelin e-posta adresi
        public string? Email { get; set; }

        // Personelin profil fotoğrafının dosya yolu
        public string? ImageUrl { get; set; }

        // Personel aktif olarak çalışıyor mu?
        public bool IsEmployeeActive { get; set; }

        // Sisteme girişte kullanılan kullanıcı adı
        public string Username { get; set; } = string.Empty;

        // Kullanıcının sistemde sahip olduğu yetki rolü
        public UserRole Role { get; set; }

        // Kullanıcı hesabı aktif mi?
        public bool IsActive { get; set; }

        // Kullanıcının sisteme son başarılı giriş zamanı
        public DateTime? LastLoginDate { get; set; }
    }
}