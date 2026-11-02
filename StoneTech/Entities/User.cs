using StoneTech.Enums;

namespace StoneTech.Entities
{
    public class User
    {
        // Kullanıcının benzersiz Id bilgisi
        public int UserId { get; set; }

        // Kullanıcı hesabının bağlı olduğu personel
        public int EmployeeId { get; set; }

        // Sisteme girişte kullanılacak kullanıcı adı
        public string Username { get; set; } = string.Empty;

        // Hashlenmiş kullanıcı şifresi
        public string PasswordHash { get; set; } = string.Empty;

        // Kullanıcının sistemdeki yetki rolü
        public UserRole Role { get; set; } = UserRole.Workshop;

        // Kullanıcının sisteme son başarılı giriş zamanı
        public DateTime? LastLoginDate { get; set; }

        // Kullanıcı hesabı aktif mi?
        public bool IsActive { get; set; } = true;

        // Kullanıcı hesabının bağlı olduğu personel
        public Employee Employee { get; set; } = null!;
    }
}