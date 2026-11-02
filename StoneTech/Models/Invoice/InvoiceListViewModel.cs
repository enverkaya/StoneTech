using StoneTech.Enums;


namespace StoneTech.Models.Invoice
{
    public class InvoiceListViewModel
    {
        //Faturanın benzersiz kimliği
        public int InvoiceId { get; set; }

        //Sistem tarafından oluşturulan fatura numarası
        // Örnek: FAT-2026-000001

        public string InvoiceNumber { get; set; } = string.Empty;

        // Faturanın oluşturulduğu iş emrinin benzersiz kimliği
        public int WorkOrderId { get; set; }

        //Faturanın bağlı olduğu iş emri numarası
        // Örnek: IE-2026-000001
        public string WorkOrderNumber { get; set; } = string.Empty;

        // Fatura üzerindeki müşteri veya firma adı
        public string CustomerName { get; set; } = string.Empty;

        // Faturanın düzenlenme tarihi
        public DateTime InvoiceDate { get; set; }

        // Faturanın KDV hariç toplam tutarı
        public decimal SubTotal { get; set; }

        // Faturanın toplam KDV tutarı
        public decimal VatAmount { get; set; }

        // Faturanın KDV dahil genel toplam tutarı
        public decimal TotalAmount { get; set; }

        // Faturanın mevcut durumu
        public InvoiceStatus Status { get; set; }


    }
}
