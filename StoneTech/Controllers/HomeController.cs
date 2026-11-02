using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoneTech.Data;
using StoneTech.Enums;
using StoneTech.Models;
using StoneTech.Models.Dashboard;

namespace StoneTech.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            AppDbContext context,
            ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Giriþ yapan kullanýcýyý rolüne ait dashboard ekranýna yönlendirir
        [HttpGet]
        public IActionResult Index()
        {
            if (User.IsInRole(UserRole.Admin.ToString())) return RedirectToAction(nameof(AdminDashboard));
            if (User.IsInRole(UserRole.Manager.ToString())) return RedirectToAction(nameof(ManagerDashboard));
            if (User.IsInRole(UserRole.Sales.ToString())) return RedirectToAction(nameof(SalesDashboard));
            if (User.IsInRole(UserRole.Accountant.ToString())) return RedirectToAction(nameof(AccountantDashboard));
            if (User.IsInRole(UserRole.Workshop.ToString())) return RedirectToAction(nameof(WorkshopDashboard));
            return RedirectToAction("AccessDenied", "Account");
        }

        // Admin dashboard ekranýna yalnýzca Admin rolü eriþebilir
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            // Bugünün tarihini ve içinde bulunduðumuz ayýn baþlangýcýný belirler
            var today = DateTime.Today;

            var monthStart = new DateTime(
                today.Year,
                today.Month,
                1);

            var nextMonthStart = monthStart.AddMonths(1);

            // Giriþ yapan kullanýcýnýn sistemdeki kullanýcý adýný alýr
            var currentUsername = User.Identity?.Name;

            // Dashboard'da gösterilecek giriþ yapan personelin adýný belirler
            var currentUserFullName = string.Empty;

            if (!string.IsNullOrWhiteSpace(currentUsername))
            {
                currentUserFullName = await _context.Users
                    .AsNoTracking()
                    .Where(x => x.Username == currentUsername)
                    .Select(x => x.Employee.FullName)
                    .FirstOrDefaultAsync()
                    ?? currentUsername;
            }

            // Kullanýcý adý bulunamazsa genel bir ifade gösterir
            if (string.IsNullOrWhiteSpace(currentUserFullName))
            {
                currentUserFullName = "Admin";
            }

            // Bu ay tamamlanan iþ emirlerinden elde edilen toplam ciroyu hesaplar
            var monthlyRevenue = await _context.WorkOrders
                .AsNoTracking()
                .Where(x =>
                    x.Status == WorkOrderStatus.Completed &&
                    x.CreatedDate >= monthStart &&
                    x.CreatedDate < nextMonthStart)
                .SumAsync(x => (decimal?)x.TotalAmount) ?? 0m;

            // Bu ay sisteme girilen toplam tahsilatý hesaplar
            var monthlyCollection = await _context.Payments
                .AsNoTracking()
                .Where(x =>
                    x.PaymentDate >= monthStart &&
                    x.PaymentDate < nextMonthStart)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            // Tamamlanan iþ emirlerinin toplam tutarýný hesaplar
            var completedWorkOrderTotal = await _context.WorkOrders
                .AsNoTracking()
                .Where(x =>
                    x.Status == WorkOrderStatus.Completed)
                .SumAsync(x => (decimal?)x.TotalAmount) ?? 0m;

            // Tamamlanan iþ emirlerine yapýlan toplam tahsilatý hesaplar
            var completedWorkOrderPaymentTotal = await _context.Payments
                .AsNoTracking()
                .Where(x =>
                    x.WorkOrder.Status == WorkOrderStatus.Completed)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            // Tamamlanan iþlerden kalan toplam alacaðý hesaplar
            var totalReceivable =
                completedWorkOrderTotal -
                completedWorkOrderPaymentTotal;

            // Negatif alacak gösterilmesini engeller
            if (totalReceivable < 0)
            {
                totalReceivable = 0;
            }

            // Sistemdeki bütün stok hareketlerinden mevcut toplam plaka sayýsýný hesaplar
            var totalStock = await _context.StockMovements
                .AsNoTracking()
                .SumAsync(x =>
                    x.MovementType == StockMovementType.Entry ||
                    x.MovementType == StockMovementType.AdjustmentIn ||
                    x.MovementType == StockMovementType.WorkOrderReturn
                        ? (decimal)x.Quantity
                        : -(decimal)x.Quantity);

            // Yönetici onayý bekleyen iþ emri sayýsýný hesaplar
            var pendingApprovalCount = await _context.WorkOrders
                .AsNoTracking()
                .CountAsync(x =>
                    x.Status == WorkOrderStatus.PendingApproval);

            // Þu anda atölyede bulunan iþ emri sayýsýný hesaplar
            var inWorkshopCount = await _context.WorkOrders
                .AsNoTracking()
                .CountAsync(x =>
                    x.Status == WorkOrderStatus.InWorkshop);

            // Bu ay tamamlanan iþ emri sayýsýný hesaplar
            var monthlyCompletedCount = await _context.WorkOrders
                .AsNoTracking()
                .CountAsync(x =>
                    x.Status == WorkOrderStatus.Completed &&
                    x.CreatedDate >= monthStart &&
                    x.CreatedDate < nextMonthStart);

            // Aktif taþlarýn mevcut stoklarýný hesaplar
            var activeStoneStocks = await _context.Stones
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Select(x => new
                {
                    Stock = x.StockMovements.Sum(m =>
                        m.MovementType == StockMovementType.Entry ||
                        m.MovementType == StockMovementType.AdjustmentIn ||
                        m.MovementType == StockMovementType.WorkOrderReturn
                            ? m.Quantity
                            : -m.Quantity)
                })
                .ToListAsync();

            // Stoðu 5 plaka veya altýnda olan aktif taþlarý kritik stok kabul eder
            var criticalStockCount = activeStoneStocks
                .Count(x => x.Stock <= 5);

            // Bu ay yapýlan toplam satýn alma tutarýný hesaplar
            var monthlyPurchaseAmount = await _context.Purchases
                .AsNoTracking()
                .Where(x =>
                    x.PurchaseDate >= monthStart &&
                    x.PurchaseDate < nextMonthStart)
                .SumAsync(x => (decimal?)x.TotalAmount) ?? 0m;

            // Bu ay oluþturulan fatura sayýsýný hesaplar
            var monthlyInvoiceCount = await _context.Invoices
                .AsNoTracking()
                .CountAsync(x =>
                    x.InvoiceDate >= monthStart &&
                    x.InvoiceDate < nextMonthStart);

            // Sistemdeki aktif müþteri sayýsýný hesaplar
            var activeCustomerCount = await _context.Customers
                .AsNoTracking()
                .CountAsync(x => x.IsActive);

            // Sistemdeki aktif tedarikçi sayýsýný hesaplar
            var activeSupplierCount = await _context.Suppliers
                .AsNoTracking()
                .CountAsync(x => x.IsActive);

            // Dashboard'da gösterilecek son 5 iþ emrini getirir
            var recentWorkOrders = await _context.WorkOrders
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedDate)
                .ThenByDescending(x => x.WorkOrderId)
                .Take(5)
                .Select(x => new AdminDashboardWorkOrderViewModel
                {
                    WorkOrderId = x.WorkOrderId,
                    WorkOrderNumber = x.WorkOrderNumber,
                    CustomerName = x.Customer.CustomerName,
                    TotalAmount = x.TotalAmount,
                    Status = x.Status,
                    CreatedDate = x.CreatedDate
                })
                .ToListAsync();

            // Dashboard'da gösterilecek son 5 tahsilatý getirir
            var recentPayments = await _context.Payments
                .AsNoTracking()
                .OrderByDescending(x => x.PaymentDate)
                .ThenByDescending(x => x.PaymentId)
                .Take(5)
                .Select(x => new AdminDashboardPaymentViewModel
                {
                    PaymentId = x.PaymentId,
                    WorkOrderId = x.WorkOrderId,
                    WorkOrderNumber = x.WorkOrder.WorkOrderNumber,
                    CustomerName = x.WorkOrder.Customer.CustomerName,
                    Amount = x.Amount,
                    PaymentMethod = x.PaymentMethod,
                    PaymentDate = x.PaymentDate
                })
                .ToListAsync();

            // Dashboard'da gösterilecek son 5 kullanýcý hesabýný getirir
            var recentUsers = await _context.Users
                .AsNoTracking()
                .OrderByDescending(x => x.UserId)
                .Take(5)
                .Select(x => new AdminDashboardUserViewModel
                {
                    UserId = x.UserId,
                    EmployeeFullName = x.Employee.FullName,
                    Username = x.Username,
                    Role = x.Role,
                    LastLoginDate = x.LastLoginDate,
                    IsActive = x.IsActive
                })
                .ToListAsync();

            // Dashboard'da gösterilecek son 5 personeli getirir
            var recentEmployees = await _context.Employees
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedDate)
                .ThenByDescending(x => x.EmployeeId)
                .Take(5)
                .Select(x => new AdminDashboardEmployeeViewModel
                {
                    EmployeeId = x.EmployeeId,
                    FullName = x.FullName,
                    Position = x.Position,
                    HireDate = x.HireDate,
                    IsActive = x.IsActive,
                    ImageUrl = x.ImageUrl
                })
                .ToListAsync();

            // Hesaplanan bütün dashboard verilerini ViewModel içerisinde toplar
            var model = new AdminDashboardViewModel
            {
                CurrentUserFullName = currentUserFullName,

                MonthlyRevenue = monthlyRevenue,
                MonthlyCollection = monthlyCollection,
                TotalReceivable = totalReceivable,
                TotalStock = totalStock,

                PendingApprovalCount = pendingApprovalCount,
                InWorkshopCount = inWorkshopCount,
                MonthlyCompletedCount = monthlyCompletedCount,
                CriticalStockCount = criticalStockCount,

                MonthlyPurchaseAmount = monthlyPurchaseAmount,
                MonthlyInvoiceCount = monthlyInvoiceCount,
                ActiveCustomerCount = activeCustomerCount,
                ActiveSupplierCount = activeSupplierCount,

                RecentWorkOrders = recentWorkOrders,
                RecentPayments = recentPayments,
                RecentUsers = recentUsers,
                RecentEmployees = recentEmployees
            };

            return View(model);
        }
        // Yönetici dashboard ekranýna yalnýzca Manager rolü eriþebilir
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> ManagerDashboard()
        {
            // Bugünün tarihini ve içinde bulunduðumuz ayýn baþlangýcýný belirler
            var today = DateTime.Today;

            var monthStart = new DateTime(
                today.Year,
                today.Month,
                1);

            var nextMonthStart = monthStart.AddMonths(1);

            // Giriþ yapan kullanýcýnýn sistemdeki kullanýcý adýný alýr
            var currentUsername = User.Identity?.Name;

            // Dashboard'da gösterilecek giriþ yapan yöneticinin adýný belirler
            var currentUserFullName = string.Empty;

            if (!string.IsNullOrWhiteSpace(currentUsername))
            {
                currentUserFullName = await _context.Users
                    .AsNoTracking()
                    .Where(x => x.Username == currentUsername)
                    .Select(x => x.Employee.FullName)
                    .FirstOrDefaultAsync()
                    ?? currentUsername;
            }

            // Kullanýcý adý bulunamazsa genel bir ifade gösterir
            if (string.IsNullOrWhiteSpace(currentUserFullName))
            {
                currentUserFullName = "Yönetici";
            }

            // Bu ay tamamlanan iþ emirlerinden elde edilen toplam ciroyu hesaplar
            var monthlyRevenue = await _context.WorkOrders
                .AsNoTracking()
                .Where(x =>
                    x.Status == WorkOrderStatus.Completed &&
                    x.CreatedDate >= monthStart &&
                    x.CreatedDate < nextMonthStart)
                .SumAsync(x => (decimal?)x.TotalAmount) ?? 0m;

            // Bu ay sisteme girilen toplam tahsilatý hesaplar
            var monthlyCollection = await _context.Payments
                .AsNoTracking()
                .Where(x =>
                    x.PaymentDate >= monthStart &&
                    x.PaymentDate < nextMonthStart)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            // Tamamlanan iþ emirlerinin toplam tutarýný hesaplar
            var completedWorkOrderTotal = await _context.WorkOrders
                .AsNoTracking()
                .Where(x =>
                    x.Status == WorkOrderStatus.Completed)
                .SumAsync(x => (decimal?)x.TotalAmount) ?? 0m;

            // Tamamlanan iþ emirlerine yapýlan toplam tahsilatý hesaplar
            var completedWorkOrderPaymentTotal = await _context.Payments
                .AsNoTracking()
                .Where(x =>
                    x.WorkOrder.Status == WorkOrderStatus.Completed)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            // Tamamlanan iþlerden kalan toplam alacaðý hesaplar
            var totalReceivable =
                completedWorkOrderTotal -
                completedWorkOrderPaymentTotal;

            // Negatif alacak gösterilmesini engeller
            if (totalReceivable < 0)
            {
                totalReceivable = 0;
            }

            // Bu ay yapýlan toplam satýn alma tutarýný hesaplar
            var monthlyPurchaseAmount = await _context.Purchases
                .AsNoTracking()
                .Where(x =>
                    x.PurchaseDate >= monthStart &&
                    x.PurchaseDate < nextMonthStart)
                .SumAsync(x => (decimal?)x.TotalAmount) ?? 0m;

            // Yönetici onayý bekleyen iþ emri sayýsýný hesaplar
            var pendingApprovalCount = await _context.WorkOrders
                .AsNoTracking()
                .CountAsync(x =>
                    x.Status == WorkOrderStatus.PendingApproval);

            // Onaylanmýþ ve atölyeye baþlamayý bekleyen iþ emri sayýsýný hesaplar
            var approvedWorkOrderCount = await _context.WorkOrders
                .AsNoTracking()
                .CountAsync(x =>
                    x.Status == WorkOrderStatus.Approved);

            // Þu anda atölyede iþlem gören iþ emri sayýsýný hesaplar
            var inWorkshopCount = await _context.WorkOrders
                .AsNoTracking()
                .CountAsync(x =>
                    x.Status == WorkOrderStatus.InWorkshop);

            // Bu ay tamamlanan iþ emri sayýsýný hesaplar
            var monthlyCompletedCount = await _context.WorkOrders
                .AsNoTracking()
                .CountAsync(x =>
                    x.Status == WorkOrderStatus.Completed &&
                    x.CreatedDate >= monthStart &&
                    x.CreatedDate < nextMonthStart);

            // Sistemdeki bütün stok hareketlerinden mevcut toplam plaka sayýsýný hesaplar
            var totalStock = await _context.StockMovements
                .AsNoTracking()
                .SumAsync(x =>
                    x.MovementType == StockMovementType.Entry ||
                    x.MovementType == StockMovementType.AdjustmentIn ||
                    x.MovementType == StockMovementType.WorkOrderReturn
                        ? (decimal)x.Quantity
                        : -(decimal)x.Quantity);

            // Aktif taþlarýn mevcut stoklarýný hesaplar
            var activeStoneStocks = await _context.Stones
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Select(x => new
                {
                    x.StoneId,
                    x.StoneName,
                    x.Color,

                    Stock = x.StockMovements.Sum(m =>
                        m.MovementType == StockMovementType.Entry ||
                        m.MovementType == StockMovementType.AdjustmentIn ||
                        m.MovementType == StockMovementType.WorkOrderReturn
                            ? m.Quantity
                            : -m.Quantity)
                })
                .ToListAsync();

            // Stoðu 5 plaka veya altýnda olan aktif taþlarý kritik stok kabul eder
            var criticalStockCount = activeStoneStocks
                .Count(x => x.Stock <= 5);

            // Bu ay oluþturulan fatura sayýsýný hesaplar
            var monthlyInvoiceCount = await _context.Invoices
                .AsNoTracking()
                .CountAsync(x =>
                    x.InvoiceDate >= monthStart &&
                    x.InvoiceDate < nextMonthStart);

            // Sistemdeki aktif müþteri sayýsýný hesaplar
            var activeCustomerCount = await _context.Customers
                .AsNoTracking()
                .CountAsync(x => x.IsActive);

            // Dashboard'da gösterilecek onay bekleyen son 5 iþ emrini getirir
            var pendingWorkOrders = await _context.WorkOrders
                .AsNoTracking()
                .Where(x =>
                    x.Status == WorkOrderStatus.PendingApproval)
                .OrderByDescending(x => x.CreatedDate)
                .ThenByDescending(x => x.WorkOrderId)
                .Take(5)
                .Select(x => new ManagerPendingWorkOrderViewModel
                {
                    WorkOrderId = x.WorkOrderId,
                    WorkOrderNumber = x.WorkOrderNumber,
                    CustomerName = x.Customer.CustomerName,
                    CreatedDate = x.CreatedDate,
                    TotalAmount = x.TotalAmount,
                    Status = x.Status
                })
                .ToListAsync();

            // Dashboard'da gösterilecek atölyedeki son 5 iþ emrini getirir
            var workshopWorkOrders = await _context.WorkOrders
                .AsNoTracking()
                .Where(x =>
                    x.Status == WorkOrderStatus.InWorkshop)
                .OrderByDescending(x => x.CreatedDate)
                .ThenByDescending(x => x.WorkOrderId)
                .Take(5)
                .Select(x => new ManagerWorkshopWorkOrderViewModel
                {
                    WorkOrderId = x.WorkOrderId,
                    WorkOrderNumber = x.WorkOrderNumber,
                    CustomerName = x.Customer.CustomerName,

                    TotalPlateQuantity = x.WorkOrderDetails
                        .Sum(d => d.Quantity),

                    CuttingMeter = x.CuttingMeter,
                    CreatedDate = x.CreatedDate,
                    Status = x.Status
                })
                .ToListAsync();

            // Tahsilatý tamamlanmamýþ iþ emirlerini hesaplar
            var receivables = await _context.WorkOrders
                .AsNoTracking()
                .Where(x =>
                    x.Status == WorkOrderStatus.Completed)
                .Select(x => new ManagerReceivableViewModel
                {
                    WorkOrderId = x.WorkOrderId,
                    WorkOrderNumber = x.WorkOrderNumber,
                    CustomerName = x.Customer.CustomerName,
                    TotalAmount = x.TotalAmount,

                    PaidAmount = x.Payments
                        .Sum(p => (decimal?)p.Amount) ?? 0m,

                    RemainingAmount =
                        x.TotalAmount -
                        (x.Payments.Sum(p => (decimal?)p.Amount) ?? 0m)
                })
                .Where(x => x.RemainingAmount > 0)
                .OrderByDescending(x => x.RemainingAmount)
                .Take(5)
                .ToListAsync();

            // Kritik stok seviyesinde bulunan taþlarý getirir
            var criticalStocks = activeStoneStocks
                .Where(x => x.Stock <= 5)
                .OrderBy(x => x.Stock)
                .ThenBy(x => x.StoneName)
                .Take(5)
                .Select(x => new ManagerCriticalStockViewModel
                {
                    StoneId = x.StoneId,
                    StoneName = x.StoneName,
                    Color = x.Color,
                    CurrentStock = x.Stock
                })
                .ToList();

            // Hesaplanan bütün yönetici dashboard verilerini ViewModel içerisinde toplar
            var model = new ManagerDashboardViewModel
            {
                CurrentUserFullName = currentUserFullName,

                MonthlyRevenue = monthlyRevenue,
                MonthlyCollection = monthlyCollection,
                TotalReceivable = totalReceivable,
                MonthlyPurchaseAmount = monthlyPurchaseAmount,

                PendingApprovalCount = pendingApprovalCount,
                ApprovedWorkOrderCount = approvedWorkOrderCount,
                InWorkshopCount = inWorkshopCount,
                MonthlyCompletedCount = monthlyCompletedCount,

                TotalStock = totalStock,
                CriticalStockCount = criticalStockCount,
                MonthlyInvoiceCount = monthlyInvoiceCount,
                ActiveCustomerCount = activeCustomerCount,

                PendingWorkOrders = pendingWorkOrders,
                WorkshopWorkOrders = workshopWorkOrders,
                Receivables = receivables,
                CriticalStocks = criticalStocks
            };

            return View(model);
        }
        // Satýþ dashboard ekranýna yalnýzca Sales rolü eriþebilir
        [Authorize(Roles = "Sales")]
        public async Task<IActionResult> SalesDashboard()
        {
            // Bugünün tarihini ve içinde bulunduðumuz ayýn baþlangýcýný belirler
            var today = DateTime.Today;

            var monthStart = new DateTime(
                today.Year,
                today.Month,
                1);

            var nextMonthStart = monthStart.AddMonths(1);

            // Giriþ yapan kullanýcýnýn sistemdeki kullanýcý adýný alýr
            var currentUsername = User.Identity?.Name;

            // Dashboard'da gösterilecek giriþ yapan satýþ personelinin adýný belirler
            var currentUserFullName = string.Empty;

            if (!string.IsNullOrWhiteSpace(currentUsername))
            {
                currentUserFullName = await _context.Users
                    .AsNoTracking()
                    .Where(x => x.Username == currentUsername)
                    .Select(x => x.Employee.FullName)
                    .FirstOrDefaultAsync()
                    ?? currentUsername;
            }

            // Kullanýcý adý bulunamazsa genel bir ifade gösterir
            if (string.IsNullOrWhiteSpace(currentUserFullName))
            {
                currentUserFullName = "Satýþ Personeli";
            }

            // Sistemdeki aktif müþteri sayýsýný hesaplar
            var activeCustomerCount = await _context.Customers
                .AsNoTracking()
                .CountAsync(x => x.IsActive);

            // Bu ay sisteme eklenen müþteri sayýsýný hesaplar
            // Customer entity'sinde oluþturulma tarihi olmadýðý için
            // bu deðer þimdilik aktif müþteri sayýsýndan ayrý hesaplanamaz
            var monthlyNewCustomerCount = 0;

            // Yönetici onayý bekleyen iþ emri sayýsýný hesaplar
            var pendingApprovalCount = await _context.WorkOrders
                .AsNoTracking()
                .CountAsync(x =>
                    x.Status == WorkOrderStatus.PendingApproval);

            // Yönetici tarafýndan onaylanmýþ iþ emri sayýsýný hesaplar
            var approvedWorkOrderCount = await _context.WorkOrders
                .AsNoTracking()
                .CountAsync(x =>
                    x.Status == WorkOrderStatus.Approved);

            // Þu anda atölyede bulunan iþ emri sayýsýný hesaplar
            var inWorkshopCount = await _context.WorkOrders
                .AsNoTracking()
                .CountAsync(x =>
                    x.Status == WorkOrderStatus.InWorkshop);

            // Bu ay tamamlanan iþ emri sayýsýný hesaplar
            var monthlyCompletedCount = await _context.WorkOrders
                .AsNoTracking()
                .CountAsync(x =>
                    x.Status == WorkOrderStatus.Completed &&
                    x.CreatedDate >= monthStart &&
                    x.CreatedDate < nextMonthStart);

            // Bu ay oluþturulan toplam iþ emri sayýsýný hesaplar
            var monthlyWorkOrderCount = await _context.WorkOrders
                .AsNoTracking()
                .CountAsync(x =>
                    x.CreatedDate >= monthStart &&
                    x.CreatedDate < nextMonthStart);

            // Bu ay oluþturulan iþ emirlerinin toplam tutarýný hesaplar
            var monthlyWorkOrderAmount = await _context.WorkOrders
                .AsNoTracking()
                .Where(x =>
                    x.CreatedDate >= monthStart &&
                    x.CreatedDate < nextMonthStart)
                .SumAsync(x => (decimal?)x.TotalAmount) ?? 0m;

            // Bu ay tamamlanan iþlerden oluþan toplam ciroyu hesaplar
            var monthlyRevenue = await _context.WorkOrders
                .AsNoTracking()
                .Where(x =>
                    x.Status == WorkOrderStatus.Completed &&
                    x.CreatedDate >= monthStart &&
                    x.CreatedDate < nextMonthStart)
                .SumAsync(x => (decimal?)x.TotalAmount) ?? 0m;

            // Tamamlanan bütün iþ emirlerinin toplam tutarýný hesaplar
            var completedWorkOrderTotal = await _context.WorkOrders
                .AsNoTracking()
                .Where(x =>
                    x.Status == WorkOrderStatus.Completed)
                .SumAsync(x => (decimal?)x.TotalAmount) ?? 0m;

            // Tamamlanan iþ emirlerine yapýlan toplam tahsilatý hesaplar
            var completedWorkOrderPaymentTotal = await _context.Payments
                .AsNoTracking()
                .Where(x =>
                    x.WorkOrder.Status == WorkOrderStatus.Completed)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            // Tamamlanan iþlerden kalan toplam alacaðý hesaplar
            var totalReceivable =
                completedWorkOrderTotal -
                completedWorkOrderPaymentTotal;

            // Negatif alacak gösterilmesini engeller
            if (totalReceivable < 0)
            {
                totalReceivable = 0;
            }

            // Bu ay oluþturulan fatura sayýsýný hesaplar
            var monthlyInvoiceCount = await _context.Invoices
                .AsNoTracking()
                .CountAsync(x =>
                    x.InvoiceDate >= monthStart &&
                    x.InvoiceDate < nextMonthStart);

            // Sistemdeki bütün stok hareketlerinden mevcut toplam plaka sayýsýný hesaplar
            var totalStock = await _context.StockMovements
                .AsNoTracking()
                .SumAsync(x =>
                    x.MovementType == StockMovementType.Entry ||
                    x.MovementType == StockMovementType.AdjustmentIn ||
                    x.MovementType == StockMovementType.WorkOrderReturn
                        ? (decimal)x.Quantity
                        : -(decimal)x.Quantity);

            // Dashboard'da gösterilecek son 5 iþ emrini getirir
            var recentWorkOrders = await _context.WorkOrders
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedDate)
                .ThenByDescending(x => x.WorkOrderId)
                .Take(5)
                .Select(x => new SalesDashboardWorkOrderViewModel
                {
                    WorkOrderId = x.WorkOrderId,
                    WorkOrderNumber = x.WorkOrderNumber,
                    CustomerName = x.Customer.CustomerName,
                    CreatedDate = x.CreatedDate,
                    TotalAmount = x.TotalAmount,
                    Status = x.Status
                })
                .ToListAsync();

            // Dashboard'da gösterilecek son 5 müþteriyi getirir
            var recentCustomers = await _context.Customers
                .AsNoTracking()
                .OrderByDescending(x => x.CustomerId)
                .Take(5)
                .Select(x => new SalesDashboardCustomerViewModel
                {
                    CustomerId = x.CustomerId,
                    CustomerName = x.CustomerName,
                    CustomerType = x.CustomerType,
                    Phone = x.Phone,
                    Email = x.Email,
                    IsActive = x.IsActive
                })
                .ToListAsync();

            // Dashboard'da gösterilecek onay bekleyen son 5 iþ emrini getirir
            var pendingWorkOrders = await _context.WorkOrders
                .AsNoTracking()
                .Where(x =>
                    x.Status == WorkOrderStatus.PendingApproval)
                .OrderByDescending(x => x.CreatedDate)
                .ThenByDescending(x => x.WorkOrderId)
                .Take(5)
                .Select(x => new SalesDashboardPendingWorkOrderViewModel
                {
                    WorkOrderId = x.WorkOrderId,
                    WorkOrderNumber = x.WorkOrderNumber,
                    CustomerName = x.Customer.CustomerName,
                    CreatedDate = x.CreatedDate,
                    TotalAmount = x.TotalAmount
                })
                .ToListAsync();

            // Dashboard'da gösterilecek tamamlanan son 5 iþ emrini getirir
            var completedWorkOrders = await _context.WorkOrders
                .AsNoTracking()
                .Where(x =>
                    x.Status == WorkOrderStatus.Completed)
                .OrderByDescending(x => x.CreatedDate)
                .ThenByDescending(x => x.WorkOrderId)
                .Take(5)
                .Select(x => new SalesDashboardCompletedWorkOrderViewModel
                {
                    WorkOrderId = x.WorkOrderId,
                    WorkOrderNumber = x.WorkOrderNumber,
                    CustomerName = x.Customer.CustomerName,
                    TotalAmount = x.TotalAmount,
                    Status = x.Status
                })
                .ToListAsync();

            // Hesaplanan bütün satýþ dashboard verilerini ViewModel içerisinde toplar
            var model = new SalesDashboardViewModel
            {
                CurrentUserFullName = currentUserFullName,

                ActiveCustomerCount = activeCustomerCount,
                MonthlyNewCustomerCount = monthlyNewCustomerCount,

                PendingApprovalCount = pendingApprovalCount,
                ApprovedWorkOrderCount = approvedWorkOrderCount,
                InWorkshopCount = inWorkshopCount,
                MonthlyCompletedCount = monthlyCompletedCount,

                MonthlyWorkOrderCount = monthlyWorkOrderCount,
                MonthlyWorkOrderAmount = monthlyWorkOrderAmount,

                MonthlyRevenue = monthlyRevenue,
                TotalReceivable = totalReceivable,

                MonthlyInvoiceCount = monthlyInvoiceCount,
                TotalStock = totalStock,

                RecentWorkOrders = recentWorkOrders,
                RecentCustomers = recentCustomers,
                PendingWorkOrders = pendingWorkOrders,
                CompletedWorkOrders = completedWorkOrders
            };

            return View(model);
        }

        // Muhasebe dashboard ekranýna yalnýzca Accountant rolü eriþebilir
        [Authorize(Roles = "Accountant")]
        public async Task<IActionResult> AccountantDashboard()
        {
            // Bugünün tarihini ve içinde bulunduðumuz ayýn baþlangýcýný belirler
            var today = DateTime.Today;

            var monthStart = new DateTime(
                today.Year,
                today.Month,
                1);

            var nextMonthStart = monthStart.AddMonths(1);

            // Giriþ yapan kullanýcýnýn sistemdeki kullanýcý adýný alýr
            var currentUsername = User.Identity?.Name;

            // Dashboard'da gösterilecek muhasebe personelinin adýný belirler
            var currentUserFullName = string.Empty;

            if (!string.IsNullOrWhiteSpace(currentUsername))
            {
                currentUserFullName = await _context.Users
                    .AsNoTracking()
                    .Where(x => x.Username == currentUsername)
                    .Select(x => x.Employee.FullName)
                    .FirstOrDefaultAsync()
                    ?? currentUsername;
            }

            // Kullanýcý adý bulunamazsa genel bir ifade gösterir
            if (string.IsNullOrWhiteSpace(currentUserFullName))
            {
                currentUserFullName = "Muhasebe";
            }

            // Bu ay tamamlanan iþlerden oluþan toplam ciroyu hesaplar
            var monthlyRevenue = await _context.WorkOrders
                .AsNoTracking()
                .Where(x =>
                    x.Status == WorkOrderStatus.Completed &&
                    x.CreatedDate >= monthStart &&
                    x.CreatedDate < nextMonthStart)
                .SumAsync(x => (decimal?)x.TotalAmount) ?? 0m;

            // Bu ay alýnan toplam tahsilatý hesaplar
            var monthlyCollection = await _context.Payments
                .AsNoTracking()
                .Where(x =>
                    x.PaymentDate >= monthStart &&
                    x.PaymentDate < nextMonthStart)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            // Tamamlanan bütün iþlerin toplam tutarýný hesaplar
            var completedWorkOrderTotal = await _context.WorkOrders
                .AsNoTracking()
                .Where(x => x.Status == WorkOrderStatus.Completed)
                .SumAsync(x => (decimal?)x.TotalAmount) ?? 0m;

            // Tamamlanan iþlere yapýlmýþ toplam tahsilatý hesaplar
            var completedWorkOrderPaymentTotal = await _context.Payments
                .AsNoTracking()
                .Where(x =>
                    x.WorkOrder.Status == WorkOrderStatus.Completed)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            // Tamamlanan iþlerden kalan toplam alacaðý hesaplar
            var totalReceivable =
                completedWorkOrderTotal -
                completedWorkOrderPaymentTotal;

            // Negatif alacak gösterilmesini engeller
            if (totalReceivable < 0)
            {
                totalReceivable = 0;
            }

            // Bu ay yapýlan toplam satýn alma tutarýný hesaplar
            var monthlyPurchaseAmount = await _context.Purchases
                .AsNoTracking()
                .Where(x =>
                    x.PurchaseDate >= monthStart &&
                    x.PurchaseDate < nextMonthStart)
                .SumAsync(x => (decimal?)x.TotalAmount) ?? 0m;

            // Bu ay oluþturulan fatura sayýsýný hesaplar
            var monthlyInvoiceCount = await _context.Invoices
                .AsNoTracking()
                .CountAsync(x =>
                    x.InvoiceDate >= monthStart &&
                    x.InvoiceDate < nextMonthStart);

            // Sistemde bulunan taslak fatura sayýsýný hesaplar
            var draftInvoiceCount = await _context.Invoices
                .AsNoTracking()
                .CountAsync(x =>
                    x.Status == InvoiceStatus.Draft);

            // Sistemde bulunan kesilmiþ fatura sayýsýný hesaplar
            var issuedInvoiceCount = await _context.Invoices
                .AsNoTracking()
                .CountAsync(x =>
                    x.Status == InvoiceStatus.Issued);

            // Sistemde bulunan iptal edilmiþ fatura sayýsýný hesaplar
            var cancelledInvoiceCount = await _context.Invoices
                .AsNoTracking()
                .CountAsync(x =>
                    x.Status == InvoiceStatus.Cancelled);

            // Tamamlanmýþ fakat henüz faturasý oluþturulmamýþ iþ emri sayýsýný hesaplar
            var uninvoicedCompletedWorkOrderCount = await _context.WorkOrders
                .AsNoTracking()
                .CountAsync(x =>
                    x.Status == WorkOrderStatus.Completed &&
                    !_context.Invoices.Any(i =>
                        i.WorkOrderId == x.WorkOrderId));

            // Tamamlanmýþ ve tahsilatý tamamen alýnmamýþ iþ emri sayýsýný hesaplar
            var receivableWorkOrderCount = await _context.WorkOrders
                .AsNoTracking()
                .Where(x =>
                    x.Status == WorkOrderStatus.Completed)
                .CountAsync(x =>
                    x.TotalAmount >
                    (x.Payments.Sum(p => (decimal?)p.Amount) ?? 0m));

            // Bu ay yapýlan tahsilat sayýsýný hesaplar
            var monthlyPaymentCount = await _context.Payments
                .AsNoTracking()
                .CountAsync(x =>
                    x.PaymentDate >= monthStart &&
                    x.PaymentDate < nextMonthStart);

            // Sistemdeki aktif tedarikçi sayýsýný hesaplar
            var activeSupplierCount = await _context.Suppliers
                .AsNoTracking()
                .CountAsync(x => x.IsActive);

            // Dashboard'da gösterilecek son 5 tahsilatý getirir
            var recentPayments = await _context.Payments
                .AsNoTracking()
                .OrderByDescending(x => x.PaymentDate)
                .ThenByDescending(x => x.PaymentId)
                .Take(5)
                .Select(x => new AccountantDashboardPaymentViewModel
                {
                    PaymentId = x.PaymentId,
                    WorkOrderId = x.WorkOrderId,
                    WorkOrderNumber = x.WorkOrder.WorkOrderNumber,
                    CustomerName = x.WorkOrder.Customer.CustomerName,
                    Amount = x.Amount,
                    PaymentMethod = x.PaymentMethod,
                    PaymentDate = x.PaymentDate
                })
                .ToListAsync();

            // Dashboard'da gösterilecek tahsilat bekleyen son 5 iþi getirir
            var receivables = await _context.WorkOrders
                .AsNoTracking()
                .Where(x =>
                    x.Status == WorkOrderStatus.Completed)
                .Select(x => new AccountantDashboardReceivableViewModel
                {
                    WorkOrderId = x.WorkOrderId,
                    WorkOrderNumber = x.WorkOrderNumber,
                    CustomerName = x.Customer.CustomerName,
                    TotalAmount = x.TotalAmount,

                    PaidAmount =
                        x.Payments.Sum(p => (decimal?)p.Amount) ?? 0m,

                    RemainingAmount =
                        x.TotalAmount -
                        (x.Payments.Sum(p => (decimal?)p.Amount) ?? 0m)
                })
                .Where(x => x.RemainingAmount > 0)
                .OrderByDescending(x => x.RemainingAmount)
                .Take(5)
                .ToListAsync();

            // Dashboard'da gösterilecek son 5 faturayý getirir
            var recentInvoices = await _context.Invoices
                .AsNoTracking()
                .OrderByDescending(x => x.InvoiceDate)
                .ThenByDescending(x => x.InvoiceId)
                .Take(5)
                .Select(x => new AccountantDashboardInvoiceViewModel
                {
                    InvoiceId = x.InvoiceId,
                    InvoiceNumber = x.InvoiceNumber,
                    WorkOrderId = x.WorkOrderId,
                    WorkOrderNumber = x.WorkOrder.WorkOrderNumber,
                    CustomerName = x.CustomerName,
                    InvoiceDate = x.InvoiceDate,
                    TotalAmount = x.TotalAmount,
                    Status = x.Status
                })
                .ToListAsync();

            // Dashboard'da gösterilecek son 5 satýn almayý getirir
            var recentPurchases = await _context.Purchases
                .AsNoTracking()
                .OrderByDescending(x => x.PurchaseDate)
                .ThenByDescending(x => x.PurchaseId)
                .Take(5)
                .Select(x => new AccountantDashboardPurchaseViewModel
                {
                    PurchaseId = x.PurchaseId,
                    PurchaseNumber = x.PurchaseNumber,
                    SupplierName = x.Supplier.SupplierName,
                    PurchaseDate = x.PurchaseDate,
                    TotalAmount = x.TotalAmount
                })
                .ToListAsync();

            // Hesaplanan bütün muhasebe dashboard verilerini ViewModel içerisinde toplar
            var model = new AccountantDashboardViewModel
            {
                CurrentUserFullName = currentUserFullName,

                MonthlyRevenue = monthlyRevenue,
                MonthlyCollection = monthlyCollection,
                TotalReceivable = totalReceivable,
                MonthlyPurchaseAmount = monthlyPurchaseAmount,

                MonthlyInvoiceCount = monthlyInvoiceCount,
                DraftInvoiceCount = draftInvoiceCount,
                IssuedInvoiceCount = issuedInvoiceCount,
                CancelledInvoiceCount = cancelledInvoiceCount,

                UninvoicedCompletedWorkOrderCount =
                    uninvoicedCompletedWorkOrderCount,

                ReceivableWorkOrderCount =
                    receivableWorkOrderCount,

                MonthlyPaymentCount = monthlyPaymentCount,
                ActiveSupplierCount = activeSupplierCount,

                RecentPayments = recentPayments,
                Receivables = receivables,
                RecentInvoices = recentInvoices,
                RecentPurchases = recentPurchases
            };

            return View(model);
        }

        // Atölye dashboard ekranýna yalnýzca Workshop rolü eriþebilir
        [Authorize(Roles = "Workshop")]
        public async Task<IActionResult> WorkshopDashboard()
        {
            // Bugünün tarihini ve içinde bulunduðumuz ayýn baþlangýcýný belirler
            var today = DateTime.Today;

            var monthStart = new DateTime(
                today.Year,
                today.Month,
                1);

            var nextMonthStart = monthStart.AddMonths(1);

            // Giriþ yapan kullanýcýnýn sistemdeki kullanýcý adýný alýr
            var currentUsername = User.Identity?.Name;

            // Dashboard'da gösterilecek atölye personelinin adýný belirler
            var currentUserFullName = string.Empty;

            if (!string.IsNullOrWhiteSpace(currentUsername))
            {
                currentUserFullName = await _context.Users
                    .AsNoTracking()
                    .Where(x => x.Username == currentUsername)
                    .Select(x => x.Employee.FullName)
                    .FirstOrDefaultAsync()
                    ?? currentUsername;
            }

            // Kullanýcý adý bulunamazsa genel bir ifade gösterir
            if (string.IsNullOrWhiteSpace(currentUserFullName))
            {
                currentUserFullName = "Atölye Personeli";
            }

            // Onaylanmýþ ve atölyeye baþlamayý bekleyen iþ emri sayýsýný hesaplar
            var approvedWorkOrderCount = await _context.WorkOrders
                .AsNoTracking()
                .CountAsync(x =>
                    x.Status == WorkOrderStatus.Approved);

            // Þu anda atölyede iþlem gören iþ emri sayýsýný hesaplar
            var inWorkshopCount = await _context.WorkOrders
                .AsNoTracking()
                .CountAsync(x =>
                    x.Status == WorkOrderStatus.InWorkshop);

            // Bu ay tamamlanan iþ emri sayýsýný hesaplar
            var monthlyCompletedCount = await _context.WorkOrders
                .AsNoTracking()
                .CountAsync(x =>
                    x.Status == WorkOrderStatus.Completed &&
                    x.CreatedDate >= monthStart &&
                    x.CreatedDate < nextMonthStart);

            // Atölyeye baþlamayý bekleyen iþlerde kullanýlacak toplam plaka sayýsýný hesaplar
            var waitingPlateQuantity = await _context.WorkOrderDetails
                .AsNoTracking()
                .Where(x =>
                    x.WorkOrder.Status == WorkOrderStatus.Approved)
                .SumAsync(x => (int?)x.Quantity) ?? 0;

            // Þu anda atölyede bulunan iþlerde kullanýlan toplam plaka sayýsýný hesaplar
            var inWorkshopPlateQuantity = await _context.WorkOrderDetails
                .AsNoTracking()
                .Where(x =>
                    x.WorkOrder.Status == WorkOrderStatus.InWorkshop)
                .SumAsync(x => (int?)x.Quantity) ?? 0;

            // Þu anda atölyede bulunan iþlerin toplam kesim metresini hesaplar
            var inWorkshopCuttingMeter = await _context.WorkOrders
                .AsNoTracking()
                .Where(x =>
                    x.Status == WorkOrderStatus.InWorkshop)
                .SumAsync(x => (decimal?)x.CuttingMeter) ?? 0m;

            // Sistemde bulunan toplam plaka sayýsýný hesaplar
            var totalStock = await _context.StockMovements
                .AsNoTracking()
                .SumAsync(x =>
                    x.MovementType == StockMovementType.Entry ||
                    x.MovementType == StockMovementType.AdjustmentIn ||
                    x.MovementType == StockMovementType.WorkOrderReturn
                        ? (decimal)x.Quantity
                        : -(decimal)x.Quantity);

            // Sistemdeki aktif taþ çeþidi sayýsýný hesaplar
            var activeStoneCount = await _context.Stones
                .AsNoTracking()
                .CountAsync(x => x.IsActive);

            // Her aktif taþýn mevcut stok miktarýný hesaplar
            var activeStoneStocks = await _context.Stones
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Select(x => new
                {
                    x.StoneId,
                    x.StoneName,
                    x.Color,
                    x.ImageUrl,

                    CurrentStock = x.StockMovements.Sum(m =>
                        m.MovementType == StockMovementType.Entry ||
                        m.MovementType == StockMovementType.AdjustmentIn ||
                        m.MovementType == StockMovementType.WorkOrderReturn
                            ? m.Quantity
                            : -m.Quantity)
                })
                .ToListAsync();

            // Stoku 5 plaka veya altýnda olan aktif taþ sayýsýný hesaplar
            var criticalStockCount = activeStoneStocks
                .Count(x => x.CurrentStock <= 5);

            // Atölyeye baþlamayý bekleyen son 5 iþ emrini getirir
            var waitingWorkOrders = await _context.WorkOrders
                .AsNoTracking()
                .Where(x =>
                    x.Status == WorkOrderStatus.Approved)
                .OrderBy(x => x.CreatedDate)
                .ThenBy(x => x.WorkOrderId)
                .Take(5)
                .Select(x => new WorkshopWaitingWorkOrderViewModel
                {
                    WorkOrderId = x.WorkOrderId,
                    WorkOrderNumber = x.WorkOrderNumber,
                    CustomerName = x.Customer.CustomerName,
                    CreatedDate = x.CreatedDate,
                    CuttingMeter = x.CuttingMeter,
                    TotalPlateQuantity =
                        x.WorkOrderDetails.Sum(d => d.Quantity),
                    Description = x.Description,

                    Stones = x.WorkOrderDetails
                        .Select(d => new WorkshopStoneViewModel
                        {
                            StoneId = d.StoneId,
                            StoneName = d.Stone.StoneName,
                            Color = d.Stone.Color,
                            ImageUrl = d.Stone.ImageUrl,
                            Quantity = d.Quantity
                        })
                        .ToList()
                })
                .ToListAsync();

            // Þu anda atölyede iþlem gören iþ emirlerini getirir
            var activeWorkOrders = await _context.WorkOrders
                .AsNoTracking()
                .Where(x =>
                    x.Status == WorkOrderStatus.InWorkshop)
                .OrderBy(x => x.CreatedDate)
                .ThenBy(x => x.WorkOrderId)
                .Take(5)
                .Select(x => new WorkshopActiveWorkOrderViewModel
                {
                    WorkOrderId = x.WorkOrderId,
                    WorkOrderNumber = x.WorkOrderNumber,
                    CustomerName = x.Customer.CustomerName,
                    CreatedDate = x.CreatedDate,
                    CuttingMeter = x.CuttingMeter,
                    TotalPlateQuantity =
                        x.WorkOrderDetails.Sum(d => d.Quantity),
                    Description = x.Description,

                    Stones = x.WorkOrderDetails
                        .Select(d => new WorkshopStoneViewModel
                        {
                            StoneId = d.StoneId,
                            StoneName = d.Stone.StoneName,
                            Color = d.Stone.Color,
                            ImageUrl = d.Stone.ImageUrl,
                            Quantity = d.Quantity
                        })
                        .ToList()
                })
                .ToListAsync();

            // Kritik stok seviyesinde bulunan ilk 5 taþý getirir
            var criticalStocks = activeStoneStocks
                .Where(x => x.CurrentStock <= 5)
                .OrderBy(x => x.CurrentStock)
                .ThenBy(x => x.StoneName)
                .Take(5)
                .Select(x => new WorkshopCriticalStockViewModel
                {
                    StoneId = x.StoneId,
                    StoneName = x.StoneName,
                    Color = x.Color,
                    ImageUrl = x.ImageUrl,
                    CurrentStock = x.CurrentStock
                })
                .ToList();

            // Son tamamlanan 5 iþ emrini getirir
            var completedWorkOrders = await _context.WorkOrders
                .AsNoTracking()
                .Where(x =>
                    x.Status == WorkOrderStatus.Completed)
                .OrderByDescending(x => x.CreatedDate)
                .ThenByDescending(x => x.WorkOrderId)
                .Take(5)
                .Select(x => new WorkshopCompletedWorkOrderViewModel
                {
                    WorkOrderId = x.WorkOrderId,
                    WorkOrderNumber = x.WorkOrderNumber,
                    CustomerName = x.Customer.CustomerName,
                    CreatedDate = x.CreatedDate,
                    CuttingMeter = x.CuttingMeter,

                    TotalPlateQuantity =
                        x.WorkOrderDetails.Sum(d => d.Quantity),

                    Stones = x.WorkOrderDetails
                        .Select(d => new WorkshopStoneViewModel
                        {
                            StoneId = d.StoneId,
                            StoneName = d.Stone.StoneName,
                            Color = d.Stone.Color,
                            ImageUrl = d.Stone.ImageUrl,
                            Quantity = d.Quantity
                        })
                        .ToList()
                })
                .ToListAsync();

            // Hesaplanan bütün atölye dashboard verilerini ViewModel içerisinde toplar
            var model = new WorkshopDashboardViewModel
            {
                CurrentUserFullName = currentUserFullName,

                ApprovedWorkOrderCount = approvedWorkOrderCount,
                InWorkshopCount = inWorkshopCount,
                MonthlyCompletedCount = monthlyCompletedCount,

                WaitingPlateQuantity = waitingPlateQuantity,
                InWorkshopPlateQuantity = inWorkshopPlateQuantity,
                InWorkshopCuttingMeter = inWorkshopCuttingMeter,

                CriticalStockCount = criticalStockCount,
                TotalStock = totalStock,
                ActiveStoneCount = activeStoneCount,

                WaitingWorkOrders = waitingWorkOrders,
                ActiveWorkOrders = activeWorkOrders,
                CriticalStocks = criticalStocks,
                CompletedWorkOrders = completedWorkOrders
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId =
                    Activity.Current?.Id ??
                    HttpContext.TraceIdentifier
            });
        }
    }
}