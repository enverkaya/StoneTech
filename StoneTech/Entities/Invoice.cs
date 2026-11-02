using StoneTech.Enums;

namespace StoneTech.Entities
{
    public class Invoice
    {
        // Faturanın benzersiz kimliği
        public int InvoiceId { get; set; }

        // Kullanıcıya gösterilecek benzersiz fatura numarası
        // Örnek: FAT-2026-000001
        public string InvoiceNumber { get; set; } = string.Empty;

        // Faturanın oluşturulduğu iş emrinin Id'si
        public int WorkOrderId { get; set; }

        // Faturanın ait olduğu müşterinin Id'si
        public int CustomerId { get; set; }

        // Faturanın düzenlenme tarihi
        public DateTime InvoiceDate { get; set; } = DateTime.Now;

        // Kaydın sistemde oluşturulduğu tarih
        public DateTime CreatedDate { get; set; } = DateTime.Now;


        // ============================================================
        // MÜŞTERİ SNAPSHOT BİLGİLERİ
        // ============================================================

        // Fatura oluşturulduğu andaki müşteri tipi
        public CustomerType CustomerType { get; set; }

        // Fatura oluşturulduğu andaki müşteri veya firma adı
        public string CustomerName { get; set; } = string.Empty;

        // Bireysel müşteri için T.C. kimlik numarası
        public string? IdentityNumber { get; set; }

        // Kurumsal müşteri için vergi numarası
        public string? TaxNumber { get; set; }

        // Kurumsal müşteri için vergi dairesi
        public string? TaxOffice { get; set; }

        // Fatura oluşturulduğu andaki müşteri adresi
        public string? Address { get; set; }


        // ============================================================
        // SATICI FİRMA SNAPSHOT BİLGİLERİ
        // ============================================================

        // Fatura oluşturulduğu andaki satıcı firma adı
        public string? SellerCompanyName { get; set; }

        // Fatura oluşturulduğu andaki satıcı firmanın vergi dairesi
        public string? SellerTaxOffice { get; set; }

        // Fatura oluşturulduğu andaki satıcı firmanın vergi numarası
        public string? SellerTaxNumber { get; set; }

        // Fatura oluşturulduğu andaki satıcı firma adresi
        public string? SellerAddress { get; set; }

        // Fatura oluşturulduğu andaki satıcı firma telefonu
        public string? SellerPhone { get; set; }

        // Fatura oluşturulduğu andaki satıcı firma e-posta adresi
        public string? SellerEmail { get; set; }

        // Fatura oluşturulduğu andaki satıcı firma internet sitesi
        public string? SellerWebsite { get; set; }

        // Fatura oluşturulduğu andaki satıcı firma logosunun yolunu saklar
        public string? SellerLogoUrl { get; set; }


        // ============================================================
        // FATURA TUTAR BİLGİLERİ
        // ============================================================

        // KDV hariç toplam tutar
        public decimal SubTotal { get; set; }

        // Faturaya uygulanan KDV oranı
        public decimal VatRate { get; set; }

        // Hesaplanan toplam KDV tutarı
        public decimal VatAmount { get; set; }

        // KDV dahil genel toplam
        public decimal TotalAmount { get; set; }


        // ============================================================
        // FATURA DURUM BİLGİLERİ
        // ============================================================

        // Faturanın mevcut durumu
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

        // Fatura ile ilgili isteğe bağlı açıklama
        public string? Description { get; set; }

        // Fatura iptal edildiğinde iptal edilme nedenini saklar
        public string? CancellationReason { get; set; }


        // ============================================================
        // İLİŞKİLER
        // ============================================================

        // Faturanın oluşturulduğu iş emri
        public WorkOrder WorkOrder { get; set; } = null!;

        // Faturanın ait olduğu müşteri
        public Customer Customer { get; set; } = null!;

        // Faturada bulunan kalemler
        public ICollection<InvoiceDetail> InvoiceDetails { get; set; }
            = new List<InvoiceDetail>();
    }
}