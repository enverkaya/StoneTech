using StoneTech.Models.Invoice;

namespace StoneTech.Services
{
    public interface IInvoicePdfService
    {
        // Kesinleştirilmiş faturayı profesyonel PDF belgesi olarak oluşturur
        byte[] CreateInvoicePdf(InvoiceDetailsViewModel model);
    }
}