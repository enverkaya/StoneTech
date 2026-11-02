using StoneTech.Enums;

namespace StoneTech.Models.Invoice
{
    public class InvoiceDetailsViewModel
    {
        // Faturanın benzersiz kimliği
        public int InvoiceId { get; set; }

        // Sistem tarafından oluşturulan fatura numarası
        // Örnek: FAT-2026-000001
        public string InvoiceNumber { get; set; } = string.Empty;

        // Faturanın oluşturulduğu iş emrinin benzersiz kimliği
        public int WorkOrderId { get; set; }

        // Faturanın bağlı olduğu iş emri numarası
        // Örnek: IE-2026-000001
        public string WorkOrderNumber { get; set; } = string.Empty;

        // Faturanın düzenlenme tarihi
        public DateTime InvoiceDate { get; set; }

        // Faturanın sistemde oluşturulduğu tarih
        public DateTime CreatedDate { get; set; }

        // Fatura oluşturulduğu andaki müşteri tipi
        public CustomerType CustomerType { get; set; }

        // Fatura oluşturulduğu andaki müşteri veya firma adı
        public string CustomerName { get; set; } = string.Empty;

        // Bireysel müşterinin T.C. kimlik numarası
        public string? IdentityNumber { get; set; }

        // Kurumsal müşterinin vergi numarası
        public string? TaxNumber { get; set; }

        // Kurumsal müşterinin vergi dairesi
        public string? TaxOffice { get; set; }

        // Fatura oluşturulduğu andaki müşteri adresi
        public string? Address { get; set; }

        // Faturanın KDV hariç ara toplamı
        public decimal SubTotal { get; set; }

        // Faturaya uygulanan KDV oranı
        public decimal VatRate { get; set; }

        // Faturanın toplam KDV tutarı
        public decimal VatAmount { get; set; }

        // Faturanın KDV dahil genel toplamı
        public decimal TotalAmount { get; set; }

        // Faturanın mevcut durumu
        public InvoiceStatus Status { get; set; }

        // Faturayı düzenleyen işletmenin bilgilerini tutar
        public InvoiceCompanyViewModel? Company { get; set; }

        // Faturayla ilgili açıklama
        public string? Description { get; set; }

        // Fatura iptal edildiğinde kaydedilen iptal nedenini tutar
        public string? CancellationReason { get; set; }

        // Faturada bulunan ürün ve hizmet kalemleri
        public List<InvoiceDetailViewModel> Details { get; set; }
            = new List<InvoiceDetailViewModel>();
    }
}