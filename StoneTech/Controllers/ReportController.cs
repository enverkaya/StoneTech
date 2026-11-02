using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoneTech.Enums;
using StoneTech.Models.Report;
using StoneTech.Services;
using StoneTech.Services.Reports;

namespace StoneTech.Controllers
{
    // Rapor merkezine yalnızca Admin, Manager ve Accountant rolleri erişebilir
    [Authorize(Roles = "Admin,Manager,Accountant")]
    public class ReportController : Controller
    {
        private readonly IReportService _reportService;
        private readonly IExcelService _excelService;
        private readonly IPdfService _pdfService;

        public ReportController(
            IReportService reportService,
            IExcelService excelService,
            IPdfService pdfService)
        {
            _reportService = reportService;
            _excelService = excelService;
            _pdfService = pdfService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            DateTime? startDate,
            DateTime? endDate)
        {
            if (!IsDateRangeValid(startDate, endDate))
                return InvalidDateRange(nameof(Index));

            var model =
                await _reportService.GetIndexReportAsync(
                    startDate,
                    endDate);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> WorkOrders(
            DateTime? startDate,
            DateTime? endDate,
            WorkOrderStatus? status,
            string? search)
        {
            if (!IsDateRangeValid(startDate, endDate))
                return InvalidDateRange(nameof(WorkOrders));

            var model =
                await _reportService.GetWorkOrderReportAsync(
                    startDate,
                    endDate,
                    status,
                    search);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Finance(
            DateTime? startDate,
            DateTime? endDate,
            string? search)
        {
            if (!IsDateRangeValid(startDate, endDate))
                return InvalidDateRange(nameof(Finance));

            var model =
                await _reportService.GetFinanceReportAsync(
                    startDate,
                    endDate,
                    search);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Stock(
            DateTime? startDate,
            DateTime? endDate,
            string? search,
            bool? isActive)
        {
            if (!IsDateRangeValid(startDate, endDate))
                return InvalidDateRange(nameof(Stock));

            var model =
                await _reportService.GetStockReportAsync(
                    startDate,
                    endDate,
                    search,
                    isActive);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Purchases(
            DateTime? startDate,
            DateTime? endDate,
            int? supplierId,
            string? search)
        {
            if (!IsDateRangeValid(startDate, endDate))
                return InvalidDateRange(nameof(Purchases));

            var model =
                await _reportService.GetPurchaseReportAsync(
                    startDate,
                    endDate,
                    supplierId,
                    search);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Customers(
            DateTime? startDate,
            DateTime? endDate,
            CustomerType? customerType,
            string? search)
        {
            if (!IsDateRangeValid(startDate, endDate))
                return InvalidDateRange(nameof(Customers));

            var model =
                await _reportService.GetCustomerReportAsync(
                    startDate,
                    endDate,
                    customerType,
                    search);

            return View(model);
        }

        // Seçilen raporu Excel veya PDF olarak dışa aktarır
        [HttpGet]
        public async Task<IActionResult> Export(
            string report,
            string format,
            DateTime? startDate,
            DateTime? endDate,
            WorkOrderStatus? status,
            string? search,
            bool? isActive,
            int? supplierId,
            CustomerType? customerType)
        {
            if (!Enum.TryParse<ReportType>(
                    report,
                    true,
                    out var reportType) ||
                !Enum.TryParse<ReportFormat>(
                    format,
                    true,
                    out var reportFormat) ||
                !Enum.IsDefined(reportType) ||
                !Enum.IsDefined(reportFormat))
            {
                return BadRequest(
                    "Geçersiz rapor veya dosya biçimi.");
            }

            if (!IsDateRangeValid(startDate, endDate))
            {
                return BadRequest(
                    "Bitiş tarihi başlangıç tarihinden önce olamaz.");
            }

            object model = reportType switch
            {
                ReportType.WorkOrders =>
                    await _reportService.GetWorkOrderReportAsync(
                        startDate,
                        endDate,
                        status,
                        search),

                ReportType.Finance =>
                    await _reportService.GetFinanceReportAsync(
                        startDate,
                        endDate,
                        search),

                ReportType.Stock =>
                    await _reportService.GetStockReportAsync(
                        startDate,
                        endDate,
                        search,
                        isActive),

                ReportType.Purchases =>
                    await _reportService.GetPurchaseReportAsync(
                        startDate,
                        endDate,
                        supplierId,
                        search),

                ReportType.Customers =>
                    await _reportService.GetCustomerReportAsync(
                        startDate,
                        endDate,
                        customerType,
                        search),

                _ => throw new ArgumentOutOfRangeException(
                    nameof(report))
            };

            byte[] content = CreateExport(
                reportType,
                reportFormat,
                model);

            string extension =
                reportFormat == ReportFormat.Excel
                    ? "xlsx"
                    : "pdf";

            string contentType =
                reportFormat == ReportFormat.Excel
                    ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                    : "application/pdf";

            string fileName =
                $"{GetFileName(reportType)}-" +
                $"{DateTime.Now:yyyyMMdd-HHmm}.{extension}";

            return File(
                content,
                contentType,
                fileName);
        }

        // Eski iş emri Excel bağlantılarının çalışmaya devam etmesini sağlar
        [HttpGet]
        public Task<IActionResult> ExportWorkOrdersExcel(
            DateTime? startDate,
            DateTime? endDate,
            WorkOrderStatus? status,
            string? search)
        {
            return Export(
                nameof(ReportType.WorkOrders),
                nameof(ReportFormat.Excel),
                startDate,
                endDate,
                status,
                search,
                null,
                null,
                null);
        }

        // Eski iş emri PDF bağlantılarının çalışmaya devam etmesini sağlar
        [HttpGet]
        public Task<IActionResult> ExportWorkOrdersPdf(
            DateTime? startDate,
            DateTime? endDate,
            WorkOrderStatus? status,
            string? search)
        {
            return Export(
                nameof(ReportType.WorkOrders),
                nameof(ReportFormat.Pdf),
                startDate,
                endDate,
                status,
                search,
                null,
                null,
                null);
        }

        // Rapor türüne ve dosya biçimine göre Excel veya PDF dosyasını oluşturur
        private byte[] CreateExport(
            ReportType report,
            ReportFormat format,
            object model)
        {
            return (report, format) switch
            {
                (ReportType.WorkOrders, ReportFormat.Excel) =>
                    _excelService.CreateWorkOrderReport(
                        (WorkOrderReportViewModel)model),

                (ReportType.WorkOrders, ReportFormat.Pdf) =>
                    _pdfService.CreateWorkOrderReport(
                        (WorkOrderReportViewModel)model),

                (ReportType.Finance, ReportFormat.Excel) =>
                    _excelService.CreateFinanceReport(
                        (FinanceReportViewModel)model),

                (ReportType.Finance, ReportFormat.Pdf) =>
                    _pdfService.CreateFinanceReport(
                        (FinanceReportViewModel)model),

                (ReportType.Stock, ReportFormat.Excel) =>
                    _excelService.CreateStockReport(
                        (StockReportViewModel)model),

                (ReportType.Stock, ReportFormat.Pdf) =>
                    _pdfService.CreateStockReport(
                        (StockReportViewModel)model),

                (ReportType.Purchases, ReportFormat.Excel) =>
                    _excelService.CreatePurchaseReport(
                        (PurchaseReportViewModel)model),

                (ReportType.Purchases, ReportFormat.Pdf) =>
                    _pdfService.CreatePurchaseReport(
                        (PurchaseReportViewModel)model),

                (ReportType.Customers, ReportFormat.Excel) =>
                    _excelService.CreateCustomerReport(
                        (CustomerReportViewModel)model),

                (ReportType.Customers, ReportFormat.Pdf) =>
                    _pdfService.CreateCustomerReport(
                        (CustomerReportViewModel)model),

                _ => throw new ArgumentOutOfRangeException(
                    nameof(report))
            };
        }

        // Başlangıç ve bitiş tarihinin geçerli olup olmadığını kontrol eder
        private static bool IsDateRangeValid(
            DateTime? startDate,
            DateTime? endDate)
        {
            return !startDate.HasValue ||
                   !endDate.HasValue ||
                   endDate.Value.Date >= startDate.Value.Date;
        }

        // Geçersiz tarih aralığında kullanıcıyı ilgili rapor ekranına yönlendirir
        private IActionResult InvalidDateRange(string action)
        {
            TempData["ErrorMessage"] =
                "Bitiş tarihi başlangıç tarihinden önce olamaz.";

            return RedirectToAction(action);
        }

        // Rapor türüne göre indirilecek dosyanın adını belirler
        private static string GetFileName(ReportType report)
        {
            return report switch
            {
                ReportType.WorkOrders => "Is-Emri-Raporu",
                ReportType.Finance => "Finans-Raporu",
                ReportType.Stock => "Stok-Raporu",
                ReportType.Purchases => "Alim-Raporu",
                ReportType.Customers => "Musteri-Raporu",
                _ => "Rapor"
            };
        }
    }
}
