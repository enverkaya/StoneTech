namespace StoneTech.Enums
{
    // Faturanın sistem içerisindeki mevcut durumunu belirtir
    public enum InvoiceStatus
    {
        // Fatura henüz kesinleştirilmemiştir
        Draft = 1,

        // Fatura düzenlenmiş ve kesinleştirilmiştir
        Issued = 2,

        // Fatura iptal edilmiştir
        Cancelled = 3
    }
}