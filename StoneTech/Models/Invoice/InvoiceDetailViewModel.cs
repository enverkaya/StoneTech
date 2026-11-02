namespace StoneTech.Models.Invoice
{
    public class InvoiceDetailViewModel
    {
        // Fatura kaleminin benzersiz kimliği
        public int InvoiceDetailId { get; set; }

        // Faturada gösterilecek ürün veya hizmet açıklaması
        // Örnek: Burdur Beji, Kesim İşçiliği
        public string Description { get; set; } = string.Empty;

        // Ürün veya hizmet miktarı
        public decimal Quantity { get; set; }

        // Miktarın birimi
        // Örnek: Plaka, Metre
        public string Unit { get; set; } = string.Empty;

        // KDV hariç birim fiyat
        public decimal UnitPrice { get; set; }

        // Kalemin KDV hariç toplam tutarı
        public decimal SubTotal { get; set; }

        // Kaleme uygulanan KDV oranı
        public decimal VatRate { get; set; }

        // Kalemin KDV tutarı
        public decimal VatAmount { get; set; }

        // Kalemin KDV dahil toplam tutarı
        public decimal TotalAmount { get; set; }
    }
}