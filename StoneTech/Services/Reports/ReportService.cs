using Microsoft.EntityFrameworkCore;
using StoneTech.Data;
using StoneTech.Enums;
using StoneTech.Models.Report;

namespace StoneTech.Services.Reports
{
    public class ReportService : IReportService
    {
        private readonly AppDbContext _context;

        public ReportService(AppDbContext context)
        {
            _context = context;
        }

        // Raporlama ana ekranýnda gösterilecek genel özet bilgilerini hazýrlar.
        public async Task<ReportIndexViewModel> GetIndexReportAsync(
            DateTime? startDate,
            DateTime? endDate)
        {
            // Ýptal ve reddedilen iþ emirlerini genel rapor toplamlarýna dahil etmiyoruz.
            var workOrderQuery = _context.WorkOrders
                .AsNoTracking()
                .Where(x =>
                    x.Status != WorkOrderStatus.Cancelled &&
                    x.Status != WorkOrderStatus.Rejected);

            // Tahsilat KPI'sý için geçerli iþ emirlerine ait ödemeleri alýyoruz.
            var paymentQuery = _context.Payments
                .AsNoTracking()
                .Where(x =>
                    x.WorkOrder.Status != WorkOrderStatus.Cancelled &&
                    x.WorkOrder.Status != WorkOrderStatus.Rejected);

            // Alým raporu için temel sorguyu oluþturuyoruz.
            var purchaseQuery = _context.Purchases
                .AsNoTracking()
                .AsQueryable();

            // Baþlangýç tarihi verilmiþse tüm sorgularý ilgili tarihten itibaren filtreliyoruz.
            if (startDate.HasValue)
            {
                var start = startDate.Value.Date;

                workOrderQuery = workOrderQuery
                    .Where(x => x.CreatedDate >= start);

                paymentQuery = paymentQuery
                    .Where(x => x.PaymentDate >= start);

                purchaseQuery = purchaseQuery
                    .Where(x => x.PurchaseDate >= start);
            }

            // Bitiþ tarihinin tamamýný kapsamak için ertesi günü hariç tutuyoruz.
            if (endDate.HasValue)
            {
                var endExclusive = endDate.Value.Date.AddDays(1);

                workOrderQuery = workOrderQuery
                    .Where(x => x.CreatedDate < endExclusive);

                paymentQuery = paymentQuery
                    .Where(x => x.PaymentDate < endExclusive);

                purchaseQuery = purchaseQuery
                    .Where(x => x.PurchaseDate < endExclusive);
            }

            // Seçilen dönemdeki iþ emirlerinin rapor için gerekli alanlarýný çekiyoruz.
            var workOrders = await workOrderQuery
                .Select(x => new
                {
                    x.WorkOrderId,
                    x.CustomerId,
                    x.TotalAmount
                })
                .ToListAsync();

            var ids = workOrders
                .Select(x => x.WorkOrderId)
                .ToList();

            // Ýþ emirlerine yapýlmýþ tüm ödemeleri iþ emri bazýnda topluyoruz.
            // Böylece kalan alacak güncel borcu gösteriyor.
            var payments = await _context.Payments
                .AsNoTracking()
                .Where(x => ids.Contains(x.WorkOrderId))
                .GroupBy(x => x.WorkOrderId)
                .Select(x => new
                {
                    WorkOrderId = x.Key,
                    Total = x.Sum(p => p.Amount)
                })
                .ToDictionaryAsync(x => x.WorkOrderId, x => x.Total);

            decimal receivable = 0;

            // Borcu bulunan farklý müþteri sayýsýný hesaplamak için HashSet kullanýyoruz.
            var debtors = new HashSet<int>();

            foreach (var order in workOrders)
            {
                var remaining =
                    order.TotalAmount -
                    payments.GetValueOrDefault(order.WorkOrderId);

                if (remaining <= 0)
                    continue;

                receivable += remaining;
                debtors.Add(order.CustomerId);
            }

            return new ReportIndexViewModel
            {
                StartDate = startDate,
                EndDate = endDate,

                TotalWorkOrderAmount = workOrders.Sum(x => x.TotalAmount),

                TotalPaymentAmount =
                    await paymentQuery.SumAsync(x => (decimal?)x.Amount) ?? 0,

                TotalReceivableAmount = receivable,

                TotalPurchaseAmount =
                    await purchaseQuery.SumAsync(x => (decimal?)x.TotalAmount) ?? 0,

                WorkOrderCount = workOrders.Count,
                PaymentCount = await paymentQuery.CountAsync(),
                PurchaseCount = await purchaseQuery.CountAsync(),
                DebtorCustomerCount = debtors.Count
            };
        }

        // Ýþ emri raporunu filtreler, ödeme bilgilerini hesaplar ve rapor modelini hazýrlar.
        public async Task<WorkOrderReportViewModel> GetWorkOrderReportAsync(
            DateTime? startDate,
            DateTime? endDate,
            WorkOrderStatus? status,
            string? search)
        {
            // Arama metnindeki gereksiz boþluklarý temizliyoruz.
            search = Normalize(search);

            var query = _context.WorkOrders
                .AsNoTracking()
                .AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(
                    x => x.CreatedDate >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1);

                query = query.Where(
                    x => x.CreatedDate < end);
            }

            if (status.HasValue)
            {
                query = query.Where(
                    x => x.Status == status.Value);
            }

            if (search != null)
            {
                query = query.Where(x =>
                    x.WorkOrderNumber.Contains(search) ||
                    x.Customer.CustomerName.Contains(search));
            }

            // Ýþ emri raporunda kullanýlacak temel bilgileri veritabanýndan çekiyoruz.
            var orders = await query
                .OrderByDescending(x => x.CreatedDate)
                .ThenByDescending(x => x.WorkOrderId)
                .Select(x => new
                {
                    x.WorkOrderId,
                    x.WorkOrderNumber,
                    x.CustomerId,
                    x.Customer.CustomerName,
                    x.CreatedDate,
                    x.Status,
                    x.SubTotal,
                    x.VatAmount,
                    x.TotalAmount,
                    x.CuttingMeter,

                    TotalStoneQuantity =
                        x.WorkOrderDetails.Sum(d => (int?)d.Quantity) ?? 0
                })
                .ToListAsync();

            var ids = orders
                .Select(x => x.WorkOrderId)
                .ToList();

            // Her iþ emrine yapýlan toplam ödemeyi hesaplýyoruz.
            var payments = await _context.Payments
                .AsNoTracking()
                .Where(x => ids.Contains(x.WorkOrderId))
                .GroupBy(x => x.WorkOrderId)
                .Select(x => new
                {
                    WorkOrderId = x.Key,
                    Total = x.Sum(p => p.Amount)
                })
                .ToDictionaryAsync(x => x.WorkOrderId, x => x.Total);

            // Veritabanýndan gelen kayýtlarý rapor satýrlarýna dönüþtürüyoruz.
            var items = orders
                .Select(x => new WorkOrderReportItemViewModel
                {
                    WorkOrderId = x.WorkOrderId,
                    WorkOrderNumber = x.WorkOrderNumber,
                    CustomerId = x.CustomerId,
                    CustomerName = x.CustomerName,
                    CreatedDate = x.CreatedDate,
                    Status = x.Status,
                    SubTotal = x.SubTotal,
                    VatAmount = x.VatAmount,
                    TotalAmount = x.TotalAmount,

                    TotalPaid =
                        payments.GetValueOrDefault(x.WorkOrderId),

                    RemainingAmount = Math.Max(
                        0,
                        x.TotalAmount -
                        payments.GetValueOrDefault(x.WorkOrderId)),

                    TotalStoneQuantity = x.TotalStoneQuantity,
                    CuttingMeter = x.CuttingMeter
                })
                .ToList();

            return new WorkOrderReportViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                Status = status,
                Search = search,

                TotalWorkOrderCount = items.Count,
                TotalWorkOrderAmount = items.Sum(x => x.TotalAmount),

                AverageWorkOrderAmount =
                    items.Count > 0
                        ? items.Average(x => x.TotalAmount)
                        : 0,

                TotalPaidAmount = items.Sum(x => x.TotalPaid),
                TotalRemainingAmount = items.Sum(x => x.RemainingAmount),

                StatusDistribution = items
                    .GroupBy(x => x.Status)
                    .Select(x => new WorkOrderStatusReportItemViewModel
                    {
                        Status = x.Key,
                        Count = x.Count(),
                        TotalAmount = x.Sum(y => y.TotalAmount)
                    })
                    .OrderBy(x => x.Status)
                    .ToList(),

                WorkOrders = items
            };
        }

        // Finans raporunda müþterilerin toplam iþ, tahsilat ve kalan borç bilgilerini hesaplar.
        public async Task<FinanceReportViewModel> GetFinanceReportAsync(
            DateTime? startDate,
            DateTime? endDate,
            string? search)
        {
            search = Normalize(search);

            // Ýptal ve reddedilen iþ emirlerini finans raporuna dahil etmiyoruz.
            var query = _context.WorkOrders
                .AsNoTracking()
                .Where(x =>
                    x.Status != WorkOrderStatus.Cancelled &&
                    x.Status != WorkOrderStatus.Rejected);

            if (startDate.HasValue)
            {
                query = query.Where(
                    x => x.CreatedDate >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1);

                query = query.Where(
                    x => x.CreatedDate < end);
            }

            if (search != null)
            {
                query = query.Where(
                    x => x.Customer.CustomerName.Contains(search));
            }

            var orders = await query
                .Select(x => new
                {
                    x.WorkOrderId,
                    x.CustomerId,
                    x.Customer.CustomerName,
                    x.TotalAmount
                })
                .ToListAsync();

            var ids = orders
                .Select(x => x.WorkOrderId)
                .ToList();

            // Seçilen iþ emirlerinin tüm tahsilatlarýný topluyoruz.
            var payments = await _context.Payments
                .AsNoTracking()
                .Where(x => ids.Contains(x.WorkOrderId))
                .GroupBy(x => x.WorkOrderId)
                .Select(x => new
                {
                    WorkOrderId = x.Key,
                    Total = x.Sum(p => p.Amount)
                })
                .ToDictionaryAsync(x => x.WorkOrderId, x => x.Total);

            // Finans bilgilerini müþteri bazýnda grupluyoruz.
            var items = orders
                .GroupBy(x => new
                {
                    x.CustomerId,
                    x.CustomerName
                })
                .Select(group =>
                {
                    var total = group.Sum(x => x.TotalAmount);

                    var paid = group.Sum(
                        x => payments.GetValueOrDefault(x.WorkOrderId));

                    return new FinanceReportItemViewModel
                    {
                        CustomerId = group.Key.CustomerId,
                        CustomerName = group.Key.CustomerName,
                        WorkOrderCount = group.Count(),
                        TotalAmount = total,
                        TotalPaid = paid,
                        RemainingAmount = Math.Max(0, total - paid)
                    };
                })
                .OrderByDescending(x => x.RemainingAmount)
                .ThenBy(x => x.CustomerName)
                .ToList();

            var totalAmount = items.Sum(x => x.TotalAmount);
            var totalPaid = items.Sum(x => x.TotalPaid);

            return new FinanceReportViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                Search = search,

                TotalWorkOrderAmount = totalAmount,
                TotalPaidAmount = totalPaid,
                TotalRemainingAmount = items.Sum(x => x.RemainingAmount),

                CollectionRate =
                    totalAmount > 0
                        ? totalPaid / totalAmount * 100
                        : 0,

                Items = items
            };
        }

        // Taþlarýn güncel stoklarýný ve seçilen dönemdeki stok hareketlerini raporlar.
        public async Task<StockReportViewModel> GetStockReportAsync(
            DateTime? startDate,
            DateTime? endDate,
            string? search,
            bool? isActive)
        {
            search = Normalize(search);

            var stones = _context.Stones
                .AsNoTracking()
                .AsQueryable();

            // Taþ adý veya renk üzerinden arama yapýyoruz.
            if (search != null)
            {
                stones = stones.Where(x =>
                    x.StoneName.Contains(search) ||
                    (x.Color != null && x.Color.Contains(search)));
            }

            // Aktif veya pasif taþ filtresi uygulanmýþsa sorguya ekliyoruz.
            if (isActive.HasValue)
            {
                stones = stones.Where(
                    x => x.IsActive == isActive.Value);
            }

            // Her taþýn güncel stok miktarýný tüm stok hareketlerinden hesaplýyoruz.
            var items = await stones
                .OrderBy(x => x.StoneName)
                .Select(x => new StockReportItemViewModel
                {
                    StoneId = x.StoneId,
                    StoneName = x.StoneName,
                    Color = x.Color,
                    IsActive = x.IsActive,

                    CurrentStock = x.StockMovements
                        .Sum(m => (int?)(
                            m.MovementType == StockMovementType.Entry ||
                            m.MovementType == StockMovementType.AdjustmentIn ||
                            m.MovementType == StockMovementType.WorkOrderReturn
                                ? m.Quantity
                                : -m.Quantity)) ?? 0
                })
                .ToListAsync();

            var ids = items
                .Select(x => x.StoneId)
                .ToList();

            // Sadece raporda bulunan taþlarýn stok hareketlerini alýyoruz.
            var movements = _context.StockMovements
                .AsNoTracking()
                .Where(x => ids.Contains(x.StoneId));

            // Dönem giriþ/çýkýþ deðerleri için tarih filtresi uyguluyoruz.
            if (startDate.HasValue)
            {
                movements = movements.Where(
                    x => x.MovementDate >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1);

                movements = movements.Where(
                    x => x.MovementDate < end);
            }

            // Stok hareketlerini taþ ve hareket türüne göre topluyoruz.
            var totals = await movements
                .GroupBy(x => x.StoneId)
                .Select(x => new
                {
                    StoneId = x.Key,

                    Entry = x
                        .Where(m =>
                            m.MovementType == StockMovementType.Entry)
                        .Sum(m => (int?)m.Quantity) ?? 0,

                    Exit = x
                        .Where(m =>
                            m.MovementType == StockMovementType.Exit)
                        .Sum(m => (int?)m.Quantity) ?? 0,

                    AdjustmentIn = x
                        .Where(m =>
                            m.MovementType == StockMovementType.AdjustmentIn)
                        .Sum(m => (int?)m.Quantity) ?? 0,

                    AdjustmentOut = x
                        .Where(m =>
                            m.MovementType == StockMovementType.AdjustmentOut)
                        .Sum(m => (int?)m.Quantity) ?? 0,

                    Return = x
                        .Where(m =>
                            m.MovementType == StockMovementType.WorkOrderReturn)
                        .Sum(m => (int?)m.Quantity) ?? 0
                })
                .ToDictionaryAsync(x => x.StoneId);

            // Hesaplanan dönem hareketlerini ilgili taþ satýrlarýna aktarýyoruz.
            foreach (var item in items)
            {
                if (!totals.TryGetValue(item.StoneId, out var total))
                    continue;

                item.TotalEntry = total.Entry;
                item.TotalExit = total.Exit;
                item.AdjustmentIn = total.AdjustmentIn;
                item.AdjustmentOut = total.AdjustmentOut;
                item.WorkOrderReturn = total.Return;
            }

            return new StockReportViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                Search = search,
                IsActive = isActive,

                StoneCount = items.Count,
                TotalCurrentStock = items.Sum(x => x.CurrentStock),
                TotalEntries = items.Sum(x => x.TotalEntry),
                TotalExits = items.Sum(x => x.TotalExit),

                TotalAdjustments =
                    items.Sum(x => x.AdjustmentIn) -
                    items.Sum(x => x.AdjustmentOut),

                TotalReturns = items.Sum(x => x.WorkOrderReturn),

                Items = items
            };
        }

        // Alým raporunu tarih, tedarikçi ve arama kriterlerine göre hazýrlar.
        public async Task<PurchaseReportViewModel> GetPurchaseReportAsync(
            DateTime? startDate,
            DateTime? endDate,
            int? supplierId,
            string? search)
        {
            search = Normalize(search);

            var query = _context.Purchases
                .AsNoTracking()
                .AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(
                    x => x.PurchaseDate >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1);

                query = query.Where(
                    x => x.PurchaseDate < end);
            }

            // Belirli bir tedarikçi seçilmiþse sadece onun alýmlarýný getiriyoruz.
            if (supplierId.HasValue)
            {
                query = query.Where(
                    x => x.SupplierId == supplierId.Value);
            }

            // Alým numarasý, tedarikçi, açýklama veya taþ adýna göre arama yapýyoruz.
            if (search != null)
            {
                query = query.Where(x =>
                    x.PurchaseNumber.Contains(search) ||
                    x.Supplier.SupplierName.Contains(search) ||
                    (x.Description != null &&
                     x.Description.Contains(search)) ||
                    x.PurchaseDetails.Any(
                        d => d.Stone.StoneName.Contains(search)));
            }

            // Alým kayýtlarýný rapor satýrlarýna dönüþtürüyoruz.
            var items = await query
                .OrderByDescending(x => x.PurchaseDate)
                .ThenByDescending(x => x.PurchaseId)
                .Select(x => new PurchaseReportItemViewModel
                {
                    PurchaseId = x.PurchaseId,
                    PurchaseNumber = x.PurchaseNumber,
                    SupplierId = x.SupplierId,
                    SupplierName = x.Supplier.SupplierName,
                    PurchaseDate = x.PurchaseDate,
                    DetailCount = x.PurchaseDetails.Count,

                    TotalQuantity =
                        x.PurchaseDetails.Sum(d => (int?)d.Quantity) ?? 0,

                    TotalAmount = x.TotalAmount,
                    Description = x.Description
                })
                .ToListAsync();

            // Tedarikçi filtresindeki seçenekleri hazýrlýyoruz.
            var suppliers = await _context.Suppliers
                .AsNoTracking()
                .OrderBy(x => x.SupplierName)
                .Select(x => new ReportOptionViewModel
                {
                    Id = x.SupplierId,
                    Name = x.SupplierName
                })
                .ToListAsync();

            return new PurchaseReportViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                SupplierId = supplierId,
                Search = search,

                PurchaseCount = items.Count,

                SupplierCount = items
                    .Select(x => x.SupplierId)
                    .Distinct()
                    .Count(),

                TotalPlateCount = items.Sum(x => x.TotalQuantity),
                TotalAmount = items.Sum(x => x.TotalAmount),

                Suppliers = suppliers,
                Items = items
            };
        }

        // Müþteri raporunda müþterilerin iþ hacmi, tahsilat ve kalan borçlarýný hesaplar.
        public async Task<CustomerReportViewModel> GetCustomerReportAsync(
            DateTime? startDate,
            DateTime? endDate,
            CustomerType? customerType,
            string? search)
        {
            search = Normalize(search);

            var customers = _context.Customers
                .AsNoTracking()
                .AsQueryable();

            // Bireysel veya kurumsal müþteri filtresini uygular.
            if (customerType.HasValue)
            {
                customers = customers.Where(
                    x => x.CustomerType == customerType.Value);
            }

            // Müþteri adý veya telefon numarasýna göre arama yapar.
            if (search != null)
            {
                customers = customers.Where(x =>
                    x.CustomerName.Contains(search) ||
                    x.Phone.Contains(search));
            }

            // Ýptal ve reddedilen iþ emirlerini müþteri finansallarýna dahil etmiyoruz.
            var orders = _context.WorkOrders
                .AsNoTracking()
                .Where(x =>
                    x.Status != WorkOrderStatus.Cancelled &&
                    x.Status != WorkOrderStatus.Rejected);

            if (startDate.HasValue)
            {
                orders = orders.Where(
                    x => x.CreatedDate >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1);

                orders = orders.Where(
                    x => x.CreatedDate < end);
            }

            // Filtreye uyan müþterilerin temel bilgilerini çekiyoruz.
            var customerList = await customers
                .OrderBy(x => x.CustomerName)
                .Select(x => new
                {
                    x.CustomerId,
                    x.CustomerName,
                    x.CustomerType,
                    x.Phone,
                    x.IsActive
                })
                .ToListAsync();

            var customerIds = customerList
                .Select(x => x.CustomerId)
                .ToList();

            // Sadece rapordaki müþterilere ait geçerli iþ emirlerini alýyoruz.
            var orderList = await orders
                .Where(x => customerIds.Contains(x.CustomerId))
                .Select(x => new
                {
                    x.WorkOrderId,
                    x.CustomerId,
                    x.TotalAmount
                })
                .ToListAsync();

            var orderIds = orderList
                .Select(x => x.WorkOrderId)
                .ToList();

            // Ýþ emirlerine yapýlan toplam ödemeleri hesaplýyoruz.
            var payments = await _context.Payments
                .AsNoTracking()
                .Where(x => orderIds.Contains(x.WorkOrderId))
                .GroupBy(x => x.WorkOrderId)
                .Select(x => new
                {
                    WorkOrderId = x.Key,
                    Total = x.Sum(p => p.Amount)
                })
                .ToDictionaryAsync(x => x.WorkOrderId, x => x.Total);

            // Ýþ emirlerini müþteri Id'sine göre grupluyoruz.
            var grouped = orderList
                .GroupBy(x => x.CustomerId)
                .ToDictionary(x => x.Key, x => x.ToList());

            // Her müþteri için toplam iþ, tahsilat ve kalan borcu hesaplýyoruz.
            var items = customerList
                .Select(customer =>
                {
                    var customerOrders =
                        grouped.GetValueOrDefault(customer.CustomerId) ?? new();

                    var total =
                        customerOrders.Sum(x => x.TotalAmount);

                    var paid = customerOrders.Sum(
                        x => payments.GetValueOrDefault(x.WorkOrderId));

                    return new CustomerReportItemViewModel
                    {
                        CustomerId = customer.CustomerId,
                        CustomerName = customer.CustomerName,
                        CustomerType = customer.CustomerType,
                        Phone = customer.Phone,
                        IsActive = customer.IsActive,
                        WorkOrderCount = customerOrders.Count,
                        TotalAmount = total,
                        TotalPaid = paid,
                        RemainingAmount = Math.Max(0, total - paid)
                    };
                })
                .OrderByDescending(x => x.TotalAmount)
                .ThenBy(x => x.CustomerName)
                .ToList();

            return new CustomerReportViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                CustomerType = customerType,
                Search = search,

                CustomerCount = items.Count,
                WorkOrderCount = items.Sum(x => x.WorkOrderCount),
                TotalBusinessVolume = items.Sum(x => x.TotalAmount),
                TotalPaid = items.Sum(x => x.TotalPaid),
                TotalRemaining = items.Sum(x => x.RemainingAmount),

                Items = items
            };
        }

        // Arama metni boþsa null döndürür, doluysa baþtaki ve sondaki boþluklarý temizler.
        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}