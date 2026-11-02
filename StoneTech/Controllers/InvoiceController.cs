using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.EntityFrameworkCore;
using StoneTech.Data;
using StoneTech.Entities;
using StoneTech.Enums;
using StoneTech.Models.Invoice;
using StoneTech.Services;

namespace StoneTech.Controllers
{
    // Fatura modülüne Admin, Manager, Accountant ve Sales rolleri erişebilir
    // Sales rolü yalnızca fatura görüntüleme işlemlerini kullanabilir
    [Authorize(Roles = "Admin,Manager,Accountant,Sales")]
    public class InvoiceController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogService _activityLogService;
        private readonly IInvoicePdfService _invoicePdfService;
        private readonly IWebHostEnvironment _environment;

        // Veritabanı bağlantısını, işlem geçmişi servisini,
        // fatura PDF servisini ve web ortamını controller'a alır
        public InvoiceController(
            AppDbContext context,
            IActivityLogService activityLogService,
            IInvoicePdfService invoicePdfService,
            IWebHostEnvironment environment)
        {
            _context = context;
            _activityLogService = activityLogService;
            _invoicePdfService = invoicePdfService;
            _environment = environment;
        }

        // Faturaları listeler
        public async Task<IActionResult> Index(
            string? search,
            int page = 1,
            int pageSize = 10)
        {
            if (page < 1)
                page = 1;

            int[] allowedPageSizes = { 5, 10, 20, 50 };

            if (!allowedPageSizes.Contains(pageSize))
                pageSize = 10;

            var query = _context.Invoices
                .AsNoTracking()
                .AsQueryable();

            // Fatura numarası, iş emri numarası veya müşteri adına göre arama yapar
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x =>
                    x.InvoiceNumber.Contains(search) ||
                    x.WorkOrder.WorkOrderNumber.Contains(search) ||
                    x.CustomerName.Contains(search));
            }

            int totalCount = await query.CountAsync();

            int totalPages = (int)Math.Ceiling(
                totalCount / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
                page = totalPages;

            var invoices = await query
                .OrderByDescending(x => x.InvoiceDate)
                .ThenByDescending(x => x.InvoiceId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new InvoiceListViewModel
                {
                    InvoiceId = x.InvoiceId,
                    InvoiceNumber = x.InvoiceNumber,

                    WorkOrderId = x.WorkOrderId,
                    WorkOrderNumber = x.WorkOrder.WorkOrderNumber,

                    CustomerName = x.CustomerName,

                    InvoiceDate = x.InvoiceDate,

                    SubTotal = x.SubTotal,
                    VatAmount = x.VatAmount,
                    TotalAmount = x.TotalAmount,

                    Status = x.Status
                })
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View(invoices);
        }

        // Tamamlanmış bir iş emri için fatura oluşturma ekranını açar
        // Sales fatura oluşturamaz
        [Authorize(Roles = "Admin,Manager,Accountant")]
        [HttpGet]
        public async Task<IActionResult> Create(int workOrderId)
        {
            // İş emrini müşteri bilgileriyle birlikte getirir
            var workOrder = await _context.WorkOrders
                .AsNoTracking()
                .Include(x => x.Customer)
                .FirstOrDefaultAsync(x => x.WorkOrderId == workOrderId);

            if (workOrder == null)
                return NotFound();

            // Yalnızca tamamlanmış iş emirleri için fatura oluşturulabilir
            if (workOrder.Status != WorkOrderStatus.Completed)
            {
                TempData["ErrorMessage"] =
                    "Yalnızca tamamlanmış iş emirleri için fatura oluşturulabilir.";

                return RedirectToAction(
                    "Details",
                    "WorkOrder",
                    new { id = workOrderId });
            }

            // Aynı iş emri için daha önce fatura oluşturulup oluşturulmadığını kontrol eder
            var existingInvoice = await _context.Invoices
                .AsNoTracking()
                .Where(x => x.WorkOrderId == workOrderId)
                .Select(x => new
                {
                    x.InvoiceId,
                    x.InvoiceNumber
                })
                .FirstOrDefaultAsync();

            if (existingInvoice != null)
            {
                TempData["ErrorMessage"] =
                    $"Bu iş emri için {existingInvoice.InvoiceNumber} numaralı fatura zaten oluşturulmuş.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = existingInvoice.InvoiceId });
            }

            DateTime invoiceDate = DateTime.Today;

            var model = new InvoiceCreateViewModel
            {
                WorkOrderId = workOrder.WorkOrderId,

                // Sıradaki fatura numarasını ekranda otomatik gösterir
                InvoiceNumber =
                    await GenerateInvoiceNumberAsync(invoiceDate),

                InvoiceDate = invoiceDate
            };

            // Create ekranında iş emri ve müşteri özetini göstermek için hazırlanır
            await LoadCreateDataAsync(workOrderId);

            return View(model);
        }

        // Tamamlanmış iş emrinden faturayı oluşturur
        // Sales fatura oluşturamaz
        [Authorize(Roles = "Admin,Manager,Accountant")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InvoiceCreateViewModel model)
        {
            // İş emrini tüm fatura kaynak bilgileriyle birlikte getirir
            var workOrder = await _context.WorkOrders
                .AsNoTracking()
                .Include(x => x.Customer)
                .Include(x => x.WorkOrderDetails)
                    .ThenInclude(x => x.Stone)
                .FirstOrDefaultAsync(x =>
                    x.WorkOrderId == model.WorkOrderId);

            if (workOrder == null)
            {
                ModelState.AddModelError(
                    nameof(model.WorkOrderId),
                    "Seçilen iş emri bulunamadı.");
            }
            else
            {
                // Yalnızca tamamlanmış iş emrinden fatura oluşturulabilir
                if (workOrder.Status != WorkOrderStatus.Completed)
                {
                    ModelState.AddModelError(
                        nameof(model.WorkOrderId),
                        "Yalnızca tamamlanmış iş emirleri için fatura oluşturulabilir.");
                }

                // Faturada en az bir ticari kalem bulunmalıdır
                bool hasStoneLine = workOrder.WorkOrderDetails.Any();

                bool hasCuttingLine =
                    workOrder.CuttingMeter > 0 &&
                    workOrder.CuttingPricePerMeter >= 0;

                if (!hasStoneLine && !hasCuttingLine)
                {
                    ModelState.AddModelError(
                        nameof(model.WorkOrderId),
                        "İş emrinde faturaya aktarılabilecek bir kalem bulunamadı.");
                }
            }

            // Aynı iş emri için ikinci faturanın oluşturulmasını engeller
            bool invoiceExists = await _context.Invoices
                .AsNoTracking()
                .AnyAsync(x => x.WorkOrderId == model.WorkOrderId);

            if (invoiceExists)
            {
                ModelState.AddModelError(
                    nameof(model.WorkOrderId),
                    "Bu iş emri için daha önce fatura oluşturulmuş.");
            }

            // Faturada kullanılacak satıcı firma bilgilerini getirir
            var companySetting = await _context.CompanySettings
                .AsNoTracking()
                .FirstOrDefaultAsync();

            // Firma ayarları olmadan fatura oluşturulmasını engeller
            if (companySetting == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Fatura oluşturabilmek için önce firma bilgileri tanımlanmalıdır.");
            }

            if (!ModelState.IsValid)
            {
                model.InvoiceNumber =
                    await GenerateInvoiceNumberAsync(model.InvoiceDate);

                if (workOrder != null)
                    await LoadCreateDataAsync(model.WorkOrderId);

                return View(model);
            }

            string? createdLogoSnapshotUrl = null;

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                // Transaction içerisinde iş emrinin durumunu tekrar kontrol eder
                var currentWorkOrder = await _context.WorkOrders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.WorkOrderId == model.WorkOrderId);

                if (currentWorkOrder == null)
                {
                    await transaction.RollbackAsync();
                    return NotFound();
                }

                if (currentWorkOrder.Status != WorkOrderStatus.Completed)
                {
                    await transaction.RollbackAsync();

                    TempData["ErrorMessage"] =
                        "İş emrinin durumu değiştiği için fatura oluşturulamadı.";

                    return RedirectToAction(
                        "Details",
                        "WorkOrder",
                        new { id = model.WorkOrderId });
                }

                // Transaction içerisinde ikinci fatura kontrolünü tekrar yapar
                bool invoiceCreatedMeanwhile = await _context.Invoices
                    .AnyAsync(x =>
                        x.WorkOrderId == model.WorkOrderId);

                if (invoiceCreatedMeanwhile)
                {
                    await transaction.RollbackAsync();

                    TempData["ErrorMessage"] =
                        "Bu iş emri için daha önce fatura oluşturulmuş.";

                    return RedirectToAction(
                        "Details",
                        "WorkOrder",
                        new { id = model.WorkOrderId });
                }

                // Transaction içerisinde firma bilgilerinin hâlâ mevcut olduğunu kontrol eder
                var currentCompanySetting = await _context.CompanySettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (currentCompanySetting == null)
                {
                    await transaction.RollbackAsync();

                    TempData["ErrorMessage"] =
                        "Firma bilgileri bulunamadığı için fatura oluşturulamadı.";

                    return RedirectToAction(
                        "Details",
                        "WorkOrder",
                        new { id = model.WorkOrderId });
                }

                // Kaydetme anında sıradaki fatura numarasını sistem yeniden oluşturur
                string invoiceNumber =
                    await GenerateInvoiceNumberAsync(model.InvoiceDate);

                // Müşteri, satıcı ve ticari tutarlar fatura oluşturulduğu andaki haliyle saklanır
                var invoice = new Invoice
                {
                    InvoiceNumber = invoiceNumber,

                    WorkOrderId = workOrder!.WorkOrderId,
                    CustomerId = workOrder.CustomerId,

                    InvoiceDate = model.InvoiceDate,
                    CreatedDate = DateTime.Now,

                    // Müşteri bilgilerini snapshot olarak faturaya kopyalar
                    CustomerType = workOrder.Customer.CustomerType,
                    CustomerName = workOrder.Customer.CustomerName,
                    IdentityNumber =
                        NormalizeNullable(workOrder.Customer.IdentityNumber),
                    TaxNumber =
                        NormalizeNullable(workOrder.Customer.TaxNumber),
                    TaxOffice =
                        NormalizeNullable(workOrder.Customer.TaxOffice),
                    Address =
                        NormalizeNullable(workOrder.Customer.Address),

                    // Satıcı firma bilgilerini snapshot olarak faturaya kopyalar
                    SellerCompanyName =
                        NormalizeNullable(currentCompanySetting.CompanyName),
                    SellerTaxOffice =
                        NormalizeNullable(currentCompanySetting.TaxOffice),
                    SellerTaxNumber =
                        NormalizeNullable(currentCompanySetting.TaxNumber),
                    SellerAddress =
                        NormalizeNullable(currentCompanySetting.Address),
                    SellerPhone =
                        NormalizeNullable(currentCompanySetting.Phone),
                    SellerEmail =
                        NormalizeNullable(currentCompanySetting.Email),
                    SellerWebsite =
                        NormalizeNullable(currentCompanySetting.Website),
                    SellerLogoUrl =
                        createdLogoSnapshotUrl =
                            CreateInvoiceLogoSnapshot(
                                currentCompanySetting.LogoUrl,
                                invoiceNumber),

                    // İş emrinin ticari toplamlarını snapshot olarak faturaya kopyalar
                    SubTotal = workOrder.SubTotal,
                    VatRate = workOrder.VatRate,
                    VatAmount = workOrder.VatAmount,
                    TotalAmount = workOrder.TotalAmount,

                    Status = InvoiceStatus.Draft,

                    Description =
                        NormalizeNullable(model.Description)
                };

                _context.Invoices.Add(invoice);

                // InvoiceId oluşması için önce fatura üst bilgisini kaydeder
                await _context.SaveChangesAsync();

                // İş emrindeki taşları fatura kalemlerine snapshot olarak aktarır
                foreach (var detail in workOrder.WorkOrderDetails)
                {
                    decimal lineSubTotal = Math.Round(
                        detail.Quantity * detail.UnitPrice,
                        2,
                        MidpointRounding.AwayFromZero);

                    decimal lineVatAmount = Math.Round(
                        lineSubTotal * workOrder.VatRate / 100m,
                        2,
                        MidpointRounding.AwayFromZero);

                    decimal lineTotal =
                        lineSubTotal + lineVatAmount;

                    string description = detail.Stone.StoneName;

                    if (!string.IsNullOrWhiteSpace(detail.Description))
                    {
                        description +=
                            $" - {detail.Description.Trim()}";
                    }

                    _context.InvoiceDetails.Add(
                        new InvoiceDetail
                        {
                            InvoiceId = invoice.InvoiceId,

                            Description = description,

                            Quantity = detail.Quantity,
                            Unit = "Plaka",

                            UnitPrice = detail.UnitPrice,

                            SubTotal = lineSubTotal,
                            VatRate = workOrder.VatRate,
                            VatAmount = lineVatAmount,
                            TotalAmount = lineTotal
                        });
                }

                // İş emrinde kesim varsa ayrı bir hizmet kalemi olarak faturaya ekler
                if (workOrder.CuttingMeter > 0)
                {
                    decimal cuttingSubTotal = Math.Round(
                        workOrder.CuttingMeter *
                        workOrder.CuttingPricePerMeter,
                        2,
                        MidpointRounding.AwayFromZero);

                    decimal cuttingVatAmount = Math.Round(
                        cuttingSubTotal *
                        workOrder.VatRate / 100m,
                        2,
                        MidpointRounding.AwayFromZero);

                    decimal cuttingTotal =
                        cuttingSubTotal + cuttingVatAmount;

                    _context.InvoiceDetails.Add(
                        new InvoiceDetail
                        {
                            InvoiceId = invoice.InvoiceId,

                            Description = "Kesim İşçiliği",

                            Quantity = workOrder.CuttingMeter,
                            Unit = "Metre",

                            UnitPrice =
                                workOrder.CuttingPricePerMeter,

                            SubTotal = cuttingSubTotal,
                            VatRate = workOrder.VatRate,
                            VatAmount = cuttingVatAmount,
                            TotalAmount = cuttingTotal
                        });
                }

                await _context.SaveChangesAsync();

                // Oluşturulan faturayı işlem geçmişine kaydeder
                await _activityLogService.LogAsync(
                    "Invoice",
                    "Create",
                    invoice.InvoiceId,
                    $"Fatura {invoice.InvoiceNumber}, " +
                    $"{workOrder.WorkOrderNumber} numaralı iş emrinden taslak olarak oluşturuldu.");

                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    $"Fatura {invoice.InvoiceNumber} taslak olarak oluşturuldu.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = invoice.InvoiceId });
            }
            catch
            {
                await transaction.RollbackAsync();

                DeleteInvoiceLogoSnapshot(createdLogoSnapshotUrl);

                ModelState.AddModelError(
                    string.Empty,
                    "Fatura oluşturulurken bir hata oluştu.");

                model.InvoiceNumber =
                    await GenerateInvoiceNumberAsync(model.InvoiceDate);

                if (workOrder != null)
                    await LoadCreateDataAsync(model.WorkOrderId);

                return View(model);
            }
        }

        // Faturanın detaylarını gösterir
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var model = await GetInvoiceDetailsViewModelAsync(id);

            if (model == null)
                return NotFound();

            return View(model);
        }

        // Kesinleştirilmiş faturayı PDF belgesi olarak oluşturur
        [HttpGet]
        public async Task<IActionResult> Pdf(int id)
        {
            var model = await GetInvoiceDetailsViewModelAsync(id);

            if (model == null)
                return NotFound();

            // Yalnızca kesinleştirilmiş faturalar PDF olarak oluşturulabilir
            if (model.Status != InvoiceStatus.Issued)
            {
                TempData["ErrorMessage"] =
                    "Yalnızca kesinleştirilmiş faturalar PDF olarak oluşturulabilir.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            // PDF oluşturulabilmesi için işletme bilgilerinin tanımlanmış olması gerekir
            if (model.Company == null)
            {
                TempData["ErrorMessage"] =
                    "Firma bilgileri bulunamadı. Önce Firma Ayarları ekranından işletme bilgilerini kaydediniz.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            try
            {
                byte[] pdfBytes =
                    _invoicePdfService.CreateInvoicePdf(model);

                string fileName =
                    $"{model.InvoiceNumber}.pdf";

                return File(
                    pdfBytes,
                    "application/pdf",
                    fileName);
            }
            catch
            {
                TempData["ErrorMessage"] =
                    "Fatura PDF'i oluşturulurken bir hata oluştu.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }
        }

        // Taslak faturayı kesinleştirir
        // Sales faturayı kesinleştiremez
        [Authorize(Roles = "Admin,Manager,Accountant")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Issue(int id)
        {
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(x => x.InvoiceId == id);

            if (invoice == null)
                return NotFound();

            // Yalnızca taslak faturalar kesinleştirilebilir
            if (invoice.Status != InvoiceStatus.Draft)
            {
                TempData["ErrorMessage"] =
                    "Yalnızca taslak faturalar düzenlenebilir.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            invoice.Status = InvoiceStatus.Issued;

            await _context.SaveChangesAsync();

            // Faturanın kesinleştirilmesini işlem geçmişine kaydeder
            await _activityLogService.LogAsync(
                "Invoice",
                "Issue",
                invoice.InvoiceId,
                $"Fatura {invoice.InvoiceNumber} düzenlendi ve kesinleştirildi.");

            TempData["SuccessMessage"] =
                $"Fatura {invoice.InvoiceNumber} düzenlendi ve kesinleştirildi.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // Faturayı silmeden iptal durumuna getirir ve iptal nedenini kaydeder
        // Sales faturayı iptal edemez
        [Authorize(Roles = "Admin,Manager,Accountant")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(
            int id,
            string cancellationReason)
        {
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(x => x.InvoiceId == id);

            if (invoice == null)
                return NotFound();

            // Daha önce iptal edilmiş fatura tekrar iptal edilemez
            if (invoice.Status == InvoiceStatus.Cancelled)
            {
                TempData["ErrorMessage"] =
                    $"Fatura {invoice.InvoiceNumber} zaten iptal edilmiş.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            // İptal nedeni boş bırakılamaz
            if (string.IsNullOrWhiteSpace(cancellationReason))
            {
                TempData["ErrorMessage"] =
                    "Fatura iptal nedeni zorunludur.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            cancellationReason = cancellationReason.Trim();

            // Veritabanındaki 500 karakter sınırını uygulama tarafında da kontrol eder
            if (cancellationReason.Length > 500)
            {
                TempData["ErrorMessage"] =
                    "Fatura iptal nedeni en fazla 500 karakter olabilir.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            // Faturayı iptal durumuna getirir ve iptal nedenini saklar
            invoice.Status = InvoiceStatus.Cancelled;
            invoice.CancellationReason = cancellationReason;

            await _context.SaveChangesAsync();

            // Fatura iptalini ve iptal nedenini işlem geçmişine kaydeder
            await _activityLogService.LogAsync(
                "Invoice",
                "Cancel",
                invoice.InvoiceId,
                $"Fatura {invoice.InvoiceNumber} iptal edildi. İptal nedeni: {cancellationReason}");

            TempData["SuccessMessage"] =
                $"Fatura {invoice.InvoiceNumber} iptal edildi.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // Create ekranında gösterilecek iş emri ve müşteri özetini hazırlar
        private async Task LoadCreateDataAsync(int workOrderId)
        {
            var workOrder = await _context.WorkOrders
                .AsNoTracking()
                .Where(x => x.WorkOrderId == workOrderId)
                .Select(x => new
                {
                    x.WorkOrderId,
                    x.WorkOrderNumber,
                    x.CreatedDate,

                    x.Customer.CustomerName,
                    x.Customer.CustomerType,
                    x.Customer.IdentityNumber,
                    x.Customer.TaxNumber,
                    x.Customer.TaxOffice,
                    x.Customer.Address,

                    x.SubTotal,
                    x.VatRate,
                    x.VatAmount,
                    x.TotalAmount,

                    StoneDetails = x.WorkOrderDetails
                        .Select(d => new
                        {
                            d.Stone.StoneName,
                            d.Quantity,
                            d.UnitPrice,
                            d.Description
                        })
                        .ToList(),

                    x.CuttingMeter,
                    x.CuttingPricePerMeter
                })
                .FirstOrDefaultAsync();

            ViewBag.WorkOrder = workOrder;
        }

        // Fatura detay ekranı ve PDF işlemi için ortak ViewModel'i hazırlar
        private async Task<InvoiceDetailsViewModel?> GetInvoiceDetailsViewModelAsync(
            int id)
        {
            // Fatura üst bilgisi, iş emri ve fatura kalemleri birlikte getirilir
            var invoice = await _context.Invoices
                .AsNoTracking()
                .Include(x => x.WorkOrder)
                .Include(x => x.InvoiceDetails)
                .FirstOrDefaultAsync(x => x.InvoiceId == id);

            if (invoice == null)
                return null;

            var model = new InvoiceDetailsViewModel
            {
                InvoiceId = invoice.InvoiceId,
                InvoiceNumber = invoice.InvoiceNumber,

                WorkOrderId = invoice.WorkOrderId,
                WorkOrderNumber =
                    invoice.WorkOrder.WorkOrderNumber,

                InvoiceDate = invoice.InvoiceDate,
                CreatedDate = invoice.CreatedDate,

                // Fatura oluşturulurken alınan müşteri snapshot bilgileri kullanılır
                CustomerType = invoice.CustomerType,
                CustomerName = invoice.CustomerName,
                IdentityNumber = invoice.IdentityNumber,
                TaxNumber = invoice.TaxNumber,
                TaxOffice = invoice.TaxOffice,
                Address = invoice.Address,

                // Fatura oluşturulurken alınan satıcı firma snapshot bilgileri kullanılır
                Company = new InvoiceCompanyViewModel
                {
                    CompanyName = invoice.SellerCompanyName ?? string.Empty,
                    TaxOffice = invoice.SellerTaxOffice ?? string.Empty,
                    TaxNumber = invoice.SellerTaxNumber ?? string.Empty,
                    Address = invoice.SellerAddress ?? string.Empty,
                    Phone = invoice.SellerPhone,
                    Email = invoice.SellerEmail,
                    Website = invoice.SellerWebsite,
                    LogoUrl = invoice.SellerLogoUrl
                },

                SubTotal = invoice.SubTotal,
                VatRate = invoice.VatRate,
                VatAmount = invoice.VatAmount,
                TotalAmount = invoice.TotalAmount,

                Status = invoice.Status,
                Description = invoice.Description,

                // Fatura iptal edilmişse kaydedilen iptal nedenini detay ekranına aktarır
                CancellationReason = invoice.CancellationReason,

                // Fatura kalemleri oluşturuldukları andaki snapshot değerleriyle aktarılır
                Details = invoice.InvoiceDetails
                    .OrderBy(x => x.InvoiceDetailId)
                    .Select(x => new InvoiceDetailViewModel
                    {
                        InvoiceDetailId = x.InvoiceDetailId,
                        Description = x.Description,
                        Quantity = x.Quantity,
                        Unit = x.Unit,
                        UnitPrice = x.UnitPrice,
                        SubTotal = x.SubTotal,
                        VatRate = x.VatRate,
                        VatAmount = x.VatAmount,
                        TotalAmount = x.TotalAmount
                    })
                    .ToList()
            };

            return model;
        }

        // Verilen tarihin yılına göre sıradaki fatura numarasını üretir
        // Örnek: FAT-2026-000001
        private async Task<string> GenerateInvoiceNumberAsync(
            DateTime invoiceDate)
        {
            int year = invoiceDate.Year;
            string prefix = $"FAT-{year}-";

            string? lastInvoiceNumber = await _context.Invoices
                .AsNoTracking()
                .Where(x =>
                    x.InvoiceNumber.StartsWith(prefix))
                .OrderByDescending(x =>
                    x.InvoiceNumber)
                .Select(x =>
                    x.InvoiceNumber)
                .FirstOrDefaultAsync();

            int nextSequence = 1;

            if (!string.IsNullOrWhiteSpace(lastInvoiceNumber))
            {
                string sequencePart =
                    lastInvoiceNumber.Substring(
                        prefix.Length);

                if (int.TryParse(
                    sequencePart,
                    out int lastSequence))
                {
                    nextSequence =
                        lastSequence + 1;
                }
            }

            return $"{prefix}{nextSequence:D6}";
        }

        // Firma logosunu faturaya özel kalıcı bir dosya olarak kopyalar
        // Böylece firma logosu daha sonra değiştirilse bile eski faturanın logosu korunur
        private string? CreateInvoiceLogoSnapshot(
            string? companyLogoUrl,
            string invoiceNumber)
        {
            if (string.IsNullOrWhiteSpace(companyLogoUrl))
                return null;

            // Yalnızca firma logo klasöründeki dosyaların kopyalanmasına izin verir
            const string companyLogoPrefix = "/uploads/company/";

            if (!companyLogoUrl.StartsWith(
                    companyLogoPrefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // Mevcut firma logosunun fiziksel dosya yolunu oluşturur
            var sourceRelativePath = companyLogoUrl
                .TrimStart('/')
                .Replace('/', Path.DirectorySeparatorChar);

            var sourcePhysicalPath = Path.Combine(
                _environment.WebRootPath,
                sourceRelativePath);

            // Kaynak logo dosyası bulunamazsa snapshot oluşturmaz
            if (!System.IO.File.Exists(sourcePhysicalPath))
                return null;

            var extension =
                Path.GetExtension(sourcePhysicalPath).ToLowerInvariant();

            // Fatura logo snapshot dosyaları ayrı klasörde tutulur
            var invoiceLogoDirectory = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "invoices",
                "logos");

            Directory.CreateDirectory(invoiceLogoDirectory);

            // Fatura numarasındaki özel karakterleri dosya adı için güvenli hale getirir
            var safeInvoiceNumber = string.Concat(
                invoiceNumber.Select(c =>
                    Path.GetInvalidFileNameChars().Contains(c)
                        ? '_'
                        : c));

            var fileName =
                $"{safeInvoiceNumber}-{Guid.NewGuid():N}{extension}";

            var destinationPhysicalPath = Path.Combine(
                invoiceLogoDirectory,
                fileName);

            // Firma logosunun bağımsız bir kopyasını fatura için oluşturur
            System.IO.File.Copy(
                sourcePhysicalPath,
                destinationPhysicalPath,
                overwrite: false);

            // Veritabanında faturaya özel logo yolunu saklamak için web yolunu döndürür
            return $"/uploads/invoices/logos/{fileName}";
        }

        // Fatura transaction'ı geri alınırsa veritabanında karşılığı kalmayan
        // logo snapshot dosyasını güvenli klasör sınırı içinde temizler.
        private void DeleteInvoiceLogoSnapshot(string? snapshotUrl)
        {
            const string invoiceLogoPrefix = "/uploads/invoices/logos/";

            if (string.IsNullOrWhiteSpace(snapshotUrl) ||
                !snapshotUrl.StartsWith(
                    invoiceLogoPrefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string fileName = Path.GetFileName(snapshotUrl);
            string filePath = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "invoices",
                "logos",
                fileName);

            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }

        // Boş veya yalnızca boşluk içeren metinleri null olarak kaydeder
        private static string? NormalizeNullable(
            string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}
