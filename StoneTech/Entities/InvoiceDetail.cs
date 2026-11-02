namespace StoneTech.Entities
{
    public class InvoiceDetail
    {
        // Fatura kaleminin benzersiz kimliği
        public int InvoiceDetailId { get; set; }

        // Kalemin ait olduğu faturanın Id'si
        public int InvoiceId { get; set; }

        // Faturada gösterilecek ürün veya hizmet açıklaması
        // Örnek: Burdur Beji veya Kesim İşçiliği
        public string Description { get; set; } = string.Empty;

        // Satılan ürün veya hizmet miktarı
        public decimal Quantity { get; set; }

        // Miktarın birimi
        // Örnek: Plaka, Metre
        public string Unit { get; set; } = string.Empty;

        // Bir adet/birim için KDV hariç satış fiyatı
        public decimal UnitPrice { get; set; }

        // Kalemin KDV hariç toplam tutarı
        public decimal SubTotal { get; set; }

        // Kaleme uygulanan KDV oranı
        public decimal VatRate { get; set; }

        // Kalemin KDV tutarı
        public decimal VatAmount { get; set; }

        // Kalemin KDV dahil toplam tutarı
        public decimal TotalAmount { get; set; }

        // Kalemin ait olduğu fatura
        public Invoice Invoice { get; set; } = null!;
    }
}