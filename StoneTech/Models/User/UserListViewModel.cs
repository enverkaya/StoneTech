using StoneTech.Enums;

namespace StoneTech.Models.User
{
    public class UserListViewModel
    {
        // Kullanıcının benzersiz Id bilgisi
        public int UserId { get; set; }

        // Kullanıcı hesabının bağlı olduğu personelin Id bilgisi
        public int EmployeeId { get; set; }

        // Personelin adı ve soyadı
        public string FullName { get; set; } = string.Empty;

        // Personelin işletmedeki görevi
        public string Position { get; set; } = string.Empty;

        // Personelin profil fotoğrafının dosya yolu
        public string? ImageUrl { get; set; }

        // Sisteme girişte kullanılan kullanıcı adı
        public string Username { get; set; } = string.Empty;

        // Kullanıcının sistemdeki yetki rolü
        public UserRole Role { get; set; }

        // Kullanıcı hesabı aktif mi?
        public bool IsActive { get; set; }

        // Kullanıcı hesabının bağlı olduğu personel aktif mi?
        public bool IsEmployeeActive { get; set; }

        // Kullanıcının sisteme son başarılı giriş zamanı
        public DateTime? LastLoginDate { get; set; }
    }
}