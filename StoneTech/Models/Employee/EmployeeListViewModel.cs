namespace StoneTech.Models.Employee
{
    public class EmployeeListViewModel
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

        // Personelin profil fotoğrafının dosya yolu
        public string? ImageUrl { get; set; }

        // Personel aktif olarak çalışıyor mu?
        public bool IsActive { get; set; }

        // Personele bağlı sistem kullanıcı hesabı var mı?
        public bool HasUserAccount { get; set; }
    }
}