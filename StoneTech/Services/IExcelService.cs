using StoneTech.Models.Report;

namespace StoneTech.Services
{
    public interface IExcelService
    {
        // İş emri raporunu Excel dosyası olarak oluşturur
        byte[] CreateWorkOrderReport(WorkOrderReportViewModel model);

        // Finans raporunu Excel dosyası olarak oluşturur
        byte[] CreateFinanceReport(FinanceReportViewModel model);
        byte[] CreateStockReport(StockReportViewModel model);
        byte[] CreatePurchaseReport(PurchaseReportViewModel model);
        byte[] CreateCustomerReport(CustomerReportViewModel model);
    }
}
