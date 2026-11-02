using StoneTech.Models.Report;


namespace StoneTech.Services
{
    public interface IPdfService
    {
        // İş emri raporunu PDF dosyası olarak oluşturur
        byte[] CreateWorkOrderReport(WorkOrderReportViewModel model);

        // Finans raporunu PDF dosyası olarak oluşturur
        byte[] CreateFinanceReport(FinanceReportViewModel model);

        // Stok raporunu PDF dosyası olarak oluşturur
        byte[] CreateStockReport(StockReportViewModel model);

        // Alım raporunu PDF dosyası olarak oluşturur
        byte[] CreatePurchaseReport(PurchaseReportViewModel model);

        // Müşteri raporunu PDF dosyası olarak oluşturur
        byte[] CreateCustomerReport(CustomerReportViewModel model);

       
    }
}