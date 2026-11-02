using StoneTech.Enums;

namespace StoneTech.Models.Employee
{
    public class EmployeeDetailsViewModel
    {
        // Personelin benzersiz Id bilgisi
        public int EmployeeId { get; set; }

        // Personelin adı ve soyadı
        public string FullName { get; set; } = string.Empty;

        // Personelin telefon numarası
        public string? Phone { get; set; }

        // Personelin e-posta adresi
        public string? Email { get; set; }

        // Personelin işletmedeki görevi
        public string Position { get; set; } = string.Empty;

        // Personelin işe giriş tarihi
        public DateTime HireDate { get; set; }

        // Personelin işten ayrılma tarihi
        public DateTime? TerminationDate { get; set; }

        // Personelle ilgili açıklama veya not
        public string? Note { get; set; }

        // Personel kaydının sisteme oluşturulduğu tarih
        public DateTime CreatedDate { get; set; }

        // Personelin profil fotoğrafının dosya yolu
        public string? ImageUrl { get; set; }

        // Personel aktif olarak çalışıyor mu?
        public bool IsActive { get; set; }

        // Personele bağlı sistem kullanıcı hesabı var mı?
        public bool HasUserAccount { get; set; }

        // Personelin sistem kullanıcı adı
        public string? Username { get; set; }

        // Personelin sistemde sahip olduğu yetki rolü
        public UserRole? Role { get; set; }

        // Personelin sistem kullanıcı hesabı aktif mi?
        public bool? IsUserActive { get; set; }

        // Personelin sisteme yaptığı son başarılı giriş zamanı
        public DateTime? LastLoginDate { get; set; }
    }
}