namespace StoneTech.Entities
{
    public class ActivityLog
    {
        // İşlem kaydının benzersiz Id bilgisi
        public int ActivityLogId { get; set; }

        // İşlemi gerçekleştiren sistem kullanıcısının Id bilgisi
        // Kullanıcı daha sonra silinse bile işlem geçmişinin kaybolmaması için nullable tutulur
        public int? UserId { get; set; }

        // İşlemi yapan kullanıcının o andaki görünen adı
        // Kullanıcının veya personelin adı sonradan değişse bile geçmiş kayıt korunur
        public string UserFullName { get; set; } = string.Empty;

        // İşlemi yapan kullanıcının o andaki kullanıcı adı
        public string Username { get; set; } = string.Empty;

        // İşlemin gerçekleştiği modül
        // Örnek: WorkOrder, Payment, Customer, Purchase
        public string Module { get; set; } = string.Empty;

        // Gerçekleştirilen işlemin türü
        // Örnek: Create, Update, Approve, Payment, Login
        public string Action { get; set; } = string.Empty;

        // İşlem yapılan kaydın benzersiz Id bilgisi
        // Her işlem belirli bir kayda bağlı olmak zorunda olmadığı için nullable tutulur
        public int? RecordId { get; set; }

        // Kullanıcıya gösterilecek işlem açıklaması
        // Örnek: "15.000 TL ödeme kaydedildi."
        public string Description { get; set; } = string.Empty;

        // İşlemin gerçekleştirildiği tarih ve saat
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // İşlemi yapan istemcinin IP adresi
        public string? IpAddress { get; set; }

        // İşlemi yapan kullanıcı hesabı
        public User? User { get; set; }
    }
}