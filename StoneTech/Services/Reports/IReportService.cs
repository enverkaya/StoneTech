using StoneTech.Enums;
using StoneTech.Models.Report;

namespace StoneTech.Services.Reports
{
    public interface IReportService
    {
        Task<ReportIndexViewModel> GetIndexReportAsync(DateTime? startDate, DateTime? endDate);
        Task<WorkOrderReportViewModel> GetWorkOrderReportAsync(DateTime? startDate, DateTime? endDate, WorkOrderStatus? status, string? search);
        Task<FinanceReportViewModel> GetFinanceReportAsync(DateTime? startDate, DateTime? endDate, string? search);
        Task<StockReportViewModel> GetStockReportAsync(DateTime? startDate, DateTime? endDate, string? search, bool? isActive);
        Task<PurchaseReportViewModel> GetPurchaseReportAsync(DateTime? startDate, DateTime? endDate, int? supplierId, string? search);
        Task<CustomerReportViewModel> GetCustomerReportAsync(DateTime? startDate, DateTime? endDate, CustomerType? customerType, string? search);
    }
}
