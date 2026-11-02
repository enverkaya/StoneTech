namespace StoneTech.Entities
{
    public class Employee
    {
        // Personelin benzersiz Id bilgisi
        public int EmployeeId { get; set; }

        // Personelin adı ve soyadı
        public string FullName { get; set; } = string.Empty;

        // Telefon numarası
        public string? Phone { get; set; }

        // E-posta adresi
        public string? Email { get; set; }

        // Personelin işletmedeki görevi
        public string Position { get; set; } = string.Empty;

        // İşe giriş tarihi
        public DateTime HireDate { get; set; } = DateTime.Today;

        // İşten ayrılma tarihi
        public DateTime? TerminationDate { get; set; }

        // Personelle ilgili not
        public string? Note { get; set; }

        // Kaydın oluşturulma tarihi
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Personel aktif olarak çalışıyor mu?
        public bool IsActive { get; set; } = true;

        // Personelin profil fotoğrafının dosya yolu
        public string? ImageUrl { get; set; }

        // Personelin sistem kullanıcı hesabı
        public User? User { get; set; }
    }
}