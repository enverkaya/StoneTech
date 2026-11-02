namespace StoneTech.Models.ActivityLog
{
    public class ActivityLogListViewModel
    {
        // İşlem geçmişi kaydının benzersiz Id bilgisi
        public int ActivityLogId { get; set; }

        // İşlemi gerçekleştiren kullanıcının görünen adı
        public string UserFullName { get; set; } = string.Empty;

        // İşlemi gerçekleştiren kullanıcının kullanıcı adı
        public string Username { get; set; } = string.Empty;

        // İşlemin gerçekleştirildiği modül
        public string Module { get; set; } = string.Empty;

        // Gerçekleştirilen işlemin türü
        public string Action { get; set; } = string.Empty;

        // İşlem yapılan kaydın Id bilgisi
        public int? RecordId { get; set; }

        // Gerçekleştirilen işlemin açıklaması
        public string Description { get; set; } = string.Empty;

        // İşlemin gerçekleştirildiği tarih ve saat
        public DateTime CreatedDate { get; set; }

        // İşlemin gerçekleştirildiği istemcinin IP adresi
        public string? IpAddress { get; set; }
    }
}