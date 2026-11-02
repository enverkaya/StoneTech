using ClosedXML.Excel;
using StoneTech.Enums;
using StoneTech.Models.Report;

namespace StoneTech.Services
{
    public class ExcelService : IExcelService
    {
        // Excel dosyalarında kullanılan ortak para biçimi
        private const string CurrencyFormat = "#,##0.00 ₺";

        // Excel dosyalarında kullanılan ortak tarih biçimi
        private const string DateFormat = "dd.MM.yyyy";

        // İş emri raporunu Excel dosyası olarak oluşturur
        public byte[] CreateWorkOrderReport(WorkOrderReportViewModel model)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("İş Emri Raporu");

            const int columnCount = 8;

            // Filtrede tarih seçilmemişse rapordaki ilk ve son iş emri tarihini kullanır
            DateTime? reportStartDate = model.StartDate;
            DateTime? reportEndDate = model.EndDate;

            if (model.WorkOrders.Any())
            {
                reportStartDate ??= model.WorkOrders.Min(x => x.CreatedDate).Date;
                reportEndDate ??= model.WorkOrders.Max(x => x.CreatedDate).Date;
            }

            // Raporun ortak üst başlık alanını oluşturur
            CreateReportHeader(
                worksheet,
                "İş Emri Raporu",
                columnCount,
                reportStartDate,
                reportEndDate);

            // Raporun özet bilgilerini oluşturur
            const int summaryStartRow = 8;

            CreateSummaryItem(
                worksheet,
                summaryStartRow,
                1,
                2,
                "Toplam İş Emri",
                model.TotalWorkOrderCount);

            CreateSummaryItem(
                worksheet,
                summaryStartRow,
                3,
                4,
                "Toplam Tutar",
                model.TotalWorkOrderAmount,
                true);

            CreateSummaryItem(
                worksheet,
                summaryStartRow,
                5,
                6,
                "Tahsil Edilen",
                model.TotalPaidAmount,
                true);

            CreateSummaryItem(
                worksheet,
                summaryStartRow,
                7,
                8,
                "Kalan Alacak",
                model.TotalRemainingAmount,
                true);

            // İş emri tablosunun başlıklarını oluşturur
            const int headerRow = 11;

            string[] headers =
            {
                "İş Emri No",
                "Müşteri",
                "Tarih",
                "Durum",
                "Plaka",
                "Kesim",
                "Toplam",
                "Ödenen"
            };

            CreateTableHeader(worksheet, headerRow, headers);

            int row = headerRow + 1;

            // İş emri kayıtlarını tabloya yazar
            foreach (var item in model.WorkOrders)
            {
                worksheet.Cell(row, 1).Value = item.WorkOrderNumber;
                worksheet.Cell(row, 2).Value = item.CustomerName;

                worksheet.Cell(row, 3).Value = item.CreatedDate;
                worksheet.Cell(row, 3).Style.NumberFormat.Format = DateFormat;

                worksheet.Cell(row, 4).Value = GetStatusText(item.Status);
                worksheet.Cell(row, 5).Value = item.TotalStoneQuantity;

                worksheet.Cell(row, 6).Value = item.CuttingMeter;
                worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00 \"m\"";

                worksheet.Cell(row, 7).Value = item.TotalAmount;
                worksheet.Cell(row, 7).Style.NumberFormat.Format = CurrencyFormat;

                worksheet.Cell(row, 8).Value = item.TotalPaid;
                worksheet.Cell(row, 8).Style.NumberFormat.Format = CurrencyFormat;

                row++;
            }

            // Tablonun en altındaki toplam satırını oluşturur
            int totalRow = row;

            worksheet.Range(totalRow, 1, totalRow, 4).Merge();
            worksheet.Cell(totalRow, 1).Value = "TOPLAM";

            worksheet.Cell(totalRow, 5).Value =
                model.WorkOrders.Sum(x => x.TotalStoneQuantity);

            worksheet.Cell(totalRow, 6).Value =
                model.WorkOrders.Sum(x => x.CuttingMeter);

            worksheet.Cell(totalRow, 6).Style.NumberFormat.Format =
                "#,##0.00 \"m\"";

            worksheet.Cell(totalRow, 7).Value =
                model.TotalWorkOrderAmount;

            worksheet.Cell(totalRow, 7).Style.NumberFormat.Format =
                CurrencyFormat;

            worksheet.Cell(totalRow, 8).Value =
                model.TotalPaidAmount;

            worksheet.Cell(totalRow, 8).Style.NumberFormat.Format =
                CurrencyFormat;

            StyleTotalRow(worksheet, totalRow, columnCount);

            // Veri tablosunun kenarlıklarını düzenler
            StyleDataTable(
                worksheet,
                headerRow,
                totalRow,
                columnCount);

            // İş emri raporuna özel kolon genişlikleri
            worksheet.Column(1).Width = 22;
            worksheet.Column(2).Width = 30;
            worksheet.Column(3).Width = 15;
            worksheet.Column(4).Width = 18;
            worksheet.Column(5).Width = 12;
            worksheet.Column(6).Width = 16;
            worksheet.Column(7).Width = 21;
            worksheet.Column(8).Width = 21;

            // Tarih kolonunu ortalar
            worksheet.Column(3).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            // Sayısal alanları sağa hizalar
            if (model.WorkOrders.Any())
            {
                worksheet.Range(
                        headerRow + 1,
                        5,
                        totalRow,
                        8)
                    .Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Right;
            }

            // Yazdırma ve görünüm ayarlarını yapar
            ApplyWorksheetSettings(
                worksheet,
                columnCount,
                totalRow);

            return SaveWorkbook(workbook);
        }

        // Finans raporunu Excel dosyası olarak oluşturur
        public byte[] CreateFinanceReport(FinanceReportViewModel model)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Finans Raporu");

            const int columnCount = 5;

            // Raporun ortak üst başlık alanını oluşturur
            CreateReportHeader(
                worksheet,
                "Finans Raporu",
                columnCount,
                model.StartDate,
                model.EndDate);

            // Finans raporunun özet bilgilerini oluşturur
            const int summaryStartRow = 8;

            CreateSummaryItem(
                worksheet,
                summaryStartRow,
                1,
                1,
                "Toplam İş Tutarı",
                model.TotalWorkOrderAmount,
                true);

            CreateSummaryItem(
                worksheet,
                summaryStartRow,
                2,
                2,
                "Toplam Tahsilat",
                model.TotalPaidAmount,
                true);

            CreateSummaryItem(
                worksheet,
                summaryStartRow,
                3,
                3,
                "Kalan Alacak",
                model.TotalRemainingAmount,
                true);

            CreateSummaryItem(
                worksheet,
                summaryStartRow,
                4,
                5,
                "Tahsilat Oranı",
                model.CollectionRate,
                false,
                true);

            // Finans tablosunun başlıklarını oluşturur
            const int headerRow = 11;

            string[] headers =
            {
                "Müşteri",
                "İş Emri Sayısı",
                "Toplam Tutar",
                "Ödenen",
                "Kalan Alacak"
            };

            CreateTableHeader(worksheet, headerRow, headers);

            int row = headerRow + 1;

            // Müşteri bazlı finans kayıtlarını tabloya yazar
            foreach (var item in model.Items)
            {
                worksheet.Cell(row, 1).Value = item.CustomerName;
                worksheet.Cell(row, 2).Value = item.WorkOrderCount;

                worksheet.Cell(row, 3).Value = item.TotalAmount;
                worksheet.Cell(row, 3).Style.NumberFormat.Format = CurrencyFormat;

                worksheet.Cell(row, 4).Value = item.TotalPaid;
                worksheet.Cell(row, 4).Style.NumberFormat.Format = CurrencyFormat;

                worksheet.Cell(row, 5).Value = item.RemainingAmount;
                worksheet.Cell(row, 5).Style.NumberFormat.Format = CurrencyFormat;

                row++;
            }

            // Finans raporunun toplam satırını oluşturur
            int totalRow = row;

            worksheet.Cell(totalRow, 1).Value = "TOPLAM";
            worksheet.Cell(totalRow, 2).Value =
                model.Items.Sum(x => x.WorkOrderCount);

            worksheet.Cell(totalRow, 3).Value =
                model.TotalWorkOrderAmount;

            worksheet.Cell(totalRow, 4).Value =
                model.TotalPaidAmount;

            worksheet.Cell(totalRow, 5).Value =
                model.TotalRemainingAmount;

            worksheet.Range(totalRow, 3, totalRow, 5)
                .Style.NumberFormat.Format = CurrencyFormat;

            StyleTotalRow(worksheet, totalRow, columnCount);

            // Tablo görünümünü düzenler
            StyleDataTable(
                worksheet,
                headerRow,
                totalRow,
                columnCount);

            // Finans raporuna özel kolon genişlikleri
            worksheet.Column(1).Width = 32;
            worksheet.Column(2).Width = 18;
            worksheet.Column(3).Width = 23;
            worksheet.Column(4).Width = 23;
            worksheet.Column(5).Width = 23;

            // Sayısal alanları sağa hizalar
            worksheet.Range(
                    headerRow + 1,
                    2,
                    totalRow,
                    5)
                .Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Right;

            // Yazdırma ve görünüm ayarlarını yapar
            ApplyWorksheetSettings(
                worksheet,
                columnCount,
                totalRow);

            return SaveWorkbook(workbook);
        }

        // Stok raporunu Excel dosyası olarak oluşturur
        public byte[] CreateStockReport(StockReportViewModel model)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Stok Raporu");

            const int columnCount = 5;

            // Raporun ortak üst başlık alanını oluşturur
            CreateReportHeader(
                worksheet,
                "Stok Raporu",
                columnCount,
                model.StartDate,
                model.EndDate);

            // Stok raporunun özet bilgilerini oluşturur
            const int firstSummaryRow = 8;
            

            CreateSummaryItem(
                worksheet,
                firstSummaryRow,
                1,
                1,
                "Taş Çeşidi",
                model.StoneCount);

            CreateSummaryItem(
                worksheet,
                firstSummaryRow,
                2,
                2,
                "Mevcut Stok",
                model.TotalCurrentStock);

            CreateSummaryItem(
                worksheet,
                firstSummaryRow,
                3,
                3,
                "Dönem Girişi",
                model.TotalEntries);

            CreateSummaryItem(
                worksheet,
                firstSummaryRow,
                4,
                4,
                "Dönem Çıkışı",
                model.TotalExits);

            CreateSummaryItem(
                worksheet,
                firstSummaryRow,
                5,
                5,
                "İade",
                model.TotalReturns);

            // Stok tablosunun başlıklarını oluşturur
            const int headerRow = 13;

            string[] headers =
            {
                "Taş",
                "Renk",
                "Mevcut Stok",
                "Dönem Girişi",
                "Dönem Çıkışı"
            };

            CreateTableHeader(worksheet, headerRow, headers);

            int row = headerRow + 1;

            // Taş bazlı stok kayıtlarını tabloya yazar
            foreach (var item in model.Items)
            {
                worksheet.Cell(row, 1).Value = item.StoneName;
                worksheet.Cell(row, 2).Value = item.Color ?? "-";
                worksheet.Cell(row, 3).Value = item.CurrentStock;
                worksheet.Cell(row, 4).Value = item.TotalEntry;
                worksheet.Cell(row, 5).Value = item.TotalExit;

                row++;
            }

            // Stok raporunun toplam satırını oluşturur
            int totalRow = row;

            worksheet.Range(totalRow, 1, totalRow, 2).Merge();
            worksheet.Cell(totalRow, 1).Value = "TOPLAM";

            worksheet.Cell(totalRow, 3).Value =
                model.TotalCurrentStock;

            worksheet.Cell(totalRow, 4).Value =
                model.TotalEntries;

            worksheet.Cell(totalRow, 5).Value =
                model.TotalExits;

            StyleTotalRow(worksheet, totalRow, columnCount);

            // Tablo görünümünü düzenler
            StyleDataTable(
                worksheet,
                headerRow,
                totalRow,
                columnCount);

            // Stok raporuna özel kolon genişlikleri
            worksheet.Column(1).Width = 34;
            worksheet.Column(2).Width = 20;
            worksheet.Column(3).Width = 18;
            worksheet.Column(4).Width = 18;
            worksheet.Column(5).Width = 18;

            // Sayısal stok alanlarını sağa hizalar
            worksheet.Range(
                    headerRow + 1,
                    3,
                    totalRow,
                    5)
                .Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Right;

            // Yazdırma ve görünüm ayarlarını yapar
            ApplyWorksheetSettings(
                worksheet,
                columnCount,
                totalRow);

            return SaveWorkbook(workbook);
        }

        // Alım raporunu Excel dosyası olarak oluşturur
        public byte[] CreatePurchaseReport(PurchaseReportViewModel model)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Alım Raporu");

            const int columnCount = 7;

            // Raporun ortak üst başlık alanını oluşturur
            CreateReportHeader(
                worksheet,
                "Alım Raporu",
                columnCount,
                model.StartDate,
                model.EndDate);

            // Alım raporunun özet bilgilerini oluşturur
            const int summaryStartRow = 8;

            CreateSummaryItem(
                worksheet,
                summaryStartRow,
                1,
                2,
                "Alım Sayısı",
                model.PurchaseCount);

            CreateSummaryItem(
                worksheet,
                summaryStartRow,
                3,
                3,
                "Tedarikçi",
                model.SupplierCount);

            CreateSummaryItem(
                worksheet,
                summaryStartRow,
                4,
                5,
                "Toplam Plaka",
                model.TotalPlateCount);

            CreateSummaryItem(
                worksheet,
                summaryStartRow,
                6,
                7,
                "Toplam Alım Tutarı",
                model.TotalAmount,
                true);

            // Alım tablosunun başlıklarını oluşturur
            const int headerRow = 11;

            string[] headers =
            {
                "Alım No",
                "Tedarikçi",
                "Tarih",
                "Kalem",
                "Plaka",
                "Toplam Tutar",
                "Açıklama"
            };

            CreateTableHeader(worksheet, headerRow, headers);

            int row = headerRow + 1;

            // Alım kayıtlarını tabloya yazar
            foreach (var item in model.Items)
            {
                worksheet.Cell(row, 1).Value = item.PurchaseNumber;
                worksheet.Cell(row, 2).Value = item.SupplierName;

                worksheet.Cell(row, 3).Value = item.PurchaseDate;
                worksheet.Cell(row, 3).Style.NumberFormat.Format = DateFormat;

                worksheet.Cell(row, 4).Value = item.DetailCount;
                worksheet.Cell(row, 5).Value = item.TotalQuantity;

                worksheet.Cell(row, 6).Value = item.TotalAmount;
                worksheet.Cell(row, 6).Style.NumberFormat.Format = CurrencyFormat;

                worksheet.Cell(row, 7).Value = item.Description ?? "-";

                row++;
            }

            // Alım raporunun toplam satırını oluşturur
            int totalRow = row;

            worksheet.Range(totalRow, 1, totalRow, 3).Merge();
            worksheet.Cell(totalRow, 1).Value = "TOPLAM";

            worksheet.Cell(totalRow, 4).Value =
                model.Items.Sum(x => x.DetailCount);

            worksheet.Cell(totalRow, 5).Value =
                model.TotalPlateCount;

            worksheet.Cell(totalRow, 6).Value =
                model.TotalAmount;

            worksheet.Cell(totalRow, 6).Style.NumberFormat.Format =
                CurrencyFormat;

            worksheet.Cell(totalRow, 7).Value = string.Empty;

            StyleTotalRow(worksheet, totalRow, columnCount);

            // Tablo görünümünü düzenler
            StyleDataTable(
                worksheet,
                headerRow,
                totalRow,
                columnCount);

            // Alım raporuna özel kolon genişlikleri
            worksheet.Column(1).Width = 23;
            worksheet.Column(2).Width = 28;
            worksheet.Column(3).Width = 16;
            worksheet.Column(4).Width = 12;
            worksheet.Column(5).Width = 12;
            worksheet.Column(6).Width = 24;
            worksheet.Column(7).Width = 38;

            // Tarih kolonunu ortalar
            worksheet.Column(3).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            // Sayısal alanları sağa hizalar
            worksheet.Range(
                    headerRow + 1,
                    4,
                    totalRow,
                    6)
                .Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Right;

            // Açıklama kolonunda uzun metinlerin taşmasını engeller
            worksheet.Column(7).Style.Alignment.WrapText = true;

            // Yazdırma ve görünüm ayarlarını yapar
            ApplyWorksheetSettings(
                worksheet,
                columnCount,
                totalRow);

            return SaveWorkbook(workbook);
        }

        // Müşteri raporunu Excel dosyası olarak oluşturur
        public byte[] CreateCustomerReport(CustomerReportViewModel model)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Müşteri Raporu");

            const int columnCount = 8;

            // Raporun ortak üst başlık alanını oluşturur
            CreateReportHeader(
                worksheet,
                "Müşteri Raporu",
                columnCount,
                model.StartDate,
                model.EndDate);

            // Müşteri raporunun ilk özet satırını oluşturur
            const int firstSummaryRow = 8;

            CreateSummaryItem(
                worksheet,
                firstSummaryRow,
                1,
                1,
                "Müşteri",
                model.CustomerCount);

            CreateSummaryItem(
                worksheet,
                firstSummaryRow,
                2,
                2,
                "İş Emri",
                model.WorkOrderCount);

            CreateSummaryItem(
                worksheet,
                firstSummaryRow,
                3,
                4,
                "İş Hacmi",
                model.TotalBusinessVolume,
                true);

            CreateSummaryItem(
                worksheet,
                firstSummaryRow,
                5,
                6,
                "Toplam Ödeme",
                model.TotalPaid,
                true);

            CreateSummaryItem(
                worksheet,
                firstSummaryRow,
                7,
                8,
                "Kalan Alacak",
                model.TotalRemaining,
                true);

            // Müşteri tablosunun başlıklarını oluşturur
            const int headerRow = 11;

            string[] headers =
            {
                "Müşteri",
                "Tip",
                "Telefon",
                "İş Emri",
                "İş Hacmi",
                "Ödeme",
                "Kalan",
                "Durum"
            };

            CreateTableHeader(worksheet, headerRow, headers);

            int row = headerRow + 1;

            // Müşteri kayıtlarını tabloya yazar
            foreach (var item in model.Items)
            {
                worksheet.Cell(row, 1).Value = item.CustomerName;

                worksheet.Cell(row, 2).Value =
                    item.CustomerType == CustomerType.Individual
                        ? "Bireysel"
                        : "Kurumsal";

                // Telefon numarasını metin olarak yazar.
                // Böylece numaranın başındaki sıfır korunur.
                worksheet.Cell(row, 3).Value = item.Phone;
                worksheet.Cell(row, 3).Style.NumberFormat.Format = "@";

                worksheet.Cell(row, 4).Value = item.WorkOrderCount;

                worksheet.Cell(row, 5).Value = item.TotalAmount;
                worksheet.Cell(row, 5).Style.NumberFormat.Format = CurrencyFormat;

                worksheet.Cell(row, 6).Value = item.TotalPaid;
                worksheet.Cell(row, 6).Style.NumberFormat.Format = CurrencyFormat;

                worksheet.Cell(row, 7).Value = item.RemainingAmount;
                worksheet.Cell(row, 7).Style.NumberFormat.Format = CurrencyFormat;

                worksheet.Cell(row, 8).Value =
                    item.IsActive ? "Aktif" : "Pasif";

                row++;
            }

            // Müşteri raporunun toplam satırını oluşturur
            int totalRow = row;

            worksheet.Range(totalRow, 1, totalRow, 3).Merge();
            worksheet.Cell(totalRow, 1).Value = "TOPLAM";

            worksheet.Cell(totalRow, 4).Value =
                model.WorkOrderCount;

            worksheet.Cell(totalRow, 5).Value =
                model.TotalBusinessVolume;

            worksheet.Cell(totalRow, 6).Value =
                model.TotalPaid;

            worksheet.Cell(totalRow, 7).Value =
                model.TotalRemaining;

            worksheet.Cell(totalRow, 8).Value = string.Empty;

            worksheet.Range(totalRow, 5, totalRow, 7)
                .Style.NumberFormat.Format = CurrencyFormat;

            StyleTotalRow(worksheet, totalRow, columnCount);

            // Tablo görünümünü düzenler
            StyleDataTable(
                worksheet,
                headerRow,
                totalRow,
                columnCount);

            // Müşteri raporuna özel kolon genişlikleri
            worksheet.Column(1).Width = 30;
            worksheet.Column(2).Width = 16;
            worksheet.Column(3).Width = 20;
            worksheet.Column(4).Width = 13;
            worksheet.Column(5).Width = 23;
            worksheet.Column(6).Width = 23;
            worksheet.Column(7).Width = 23;
            worksheet.Column(8).Width = 20;

            // Sayısal alanları sağa hizalar
            worksheet.Range(
                    headerRow + 1,
                    4,
                    totalRow,
                    7)
                .Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Right;

            // Yazdırma ve görünüm ayarlarını yapar
            ApplyWorksheetSettings(
                worksheet,
                columnCount,
                totalRow);

            return SaveWorkbook(workbook);
        }

        // TECHSTONE başlığını, rapor adını ve tarih bilgilerini oluşturur
        private static void CreateReportHeader(
            IXLWorksheet worksheet,
            string reportTitle,
            int columnCount,
            DateTime? startDate,
            DateTime? endDate)
        {
            // Firma adı
            worksheet.Cell(1, 1).Value = "TECHSTONE";

            var companyRange =
                worksheet.Range(1, 1, 1, columnCount);

            companyRange.Merge();

            companyRange.Style.Font.Bold = true;
            companyRange.Style.Font.FontSize = 18;
            companyRange.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            // Rapor adı
            worksheet.Cell(2, 1).Value = reportTitle;

            var titleRange =
                worksheet.Range(2, 1, 2, columnCount);

            titleRange.Merge();

            titleRange.Style.Font.Bold = true;
            titleRange.Style.Font.FontSize = 14;
            titleRange.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            // Başlangıç tarihi
            worksheet.Cell(4, 1).Value = "Başlangıç Tarihi";
            worksheet.Cell(4, 2).Value =
                startDate?.ToString(DateFormat) ?? "Tüm Zamanlar";

            // Bitiş tarihi
            worksheet.Cell(5, 1).Value = "Bitiş Tarihi";
            worksheet.Cell(5, 2).Value =
                endDate?.ToString(DateFormat) ?? "Tüm Zamanlar";

            // Raporun oluşturulduğu tarih ve saat
            int reportDateColumn =
                Math.Max(3, columnCount - 1);

            worksheet.Cell(4, reportDateColumn).Value =
                "Rapor Tarihi";

            worksheet.Cell(4, reportDateColumn + 1).Value =
                DateTime.Now;

            worksheet.Cell(4, reportDateColumn + 1)
                .Style.NumberFormat.Format =
                "dd.MM.yyyy HH:mm";

            // Bilgi başlıklarını kalın gösterir
            worksheet.Cell(4, 1).Style.Font.Bold = true;
            worksheet.Cell(5, 1).Style.Font.Bold = true;
            worksheet.Cell(4, reportDateColumn).Style.Font.Bold = true;

            // Tarih bilgisinin bulunduğu satırları dikey olarak ortalar
            worksheet.Range(4, 1, 5, columnCount)
                .Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;
        }

        // Raporun üst bölümündeki özet kutusunu oluşturur
        private static void CreateSummaryItem(
            IXLWorksheet worksheet,
            int startRow,
            int startColumn,
            int endColumn,
            string label,
            object value,
            bool isCurrency = false,
            bool isPercentage = false)
        {
            // Özet başlığının kapsayacağı hücreleri birleştirir
            var labelRange =
                worksheet.Range(
                    startRow,
                    startColumn,
                    startRow,
                    endColumn);

            labelRange.Merge();

            worksheet.Cell(startRow, startColumn).Value =
                label;

            // Özet değerinin kapsayacağı hücreleri birleştirir
            var valueRange =
                worksheet.Range(
                    startRow + 1,
                    startColumn,
                    startRow + 1,
                    endColumn);

            valueRange.Merge();

            SetCellValue(
                worksheet.Cell(startRow + 1, startColumn),
                value);

            // Özet başlığını biçimlendirir
            labelRange.Style.Font.Bold = true;
            labelRange.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            labelRange.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            // Özet değerini biçimlendirir
            valueRange.Style.Font.Bold = true;
            valueRange.Style.Font.FontSize = 12;

            valueRange.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            valueRange.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            // Para değerlerinde TL biçimini uygular
            if (isCurrency)
            {
                worksheet.Cell(startRow + 1, startColumn)
                    .Style.NumberFormat.Format =
                    CurrencyFormat;
            }

            // Yüzdelik değerlerde yüzde işaretini uygular
            if (isPercentage)
            {
                worksheet.Cell(startRow + 1, startColumn)
                    .Style.NumberFormat.Format =
                    "0.00\"%\"";
            }

            // Özet kutusunun kenarlıklarını oluşturur
            var summaryRange =
                worksheet.Range(
                    startRow,
                    startColumn,
                    startRow + 1,
                    endColumn);

            summaryRange.Style.Border.OutsideBorder =
                XLBorderStyleValues.Thin;

            summaryRange.Style.Border.InsideBorder =
                XLBorderStyleValues.Hair;
        }

        // Tablo kolon başlıklarını oluşturur ve ortak biçimi uygular
        private static void CreateTableHeader(
            IXLWorksheet worksheet,
            int headerRow,
            string[] headers)
        {
            for (int column = 0; column < headers.Length; column++)
            {
                worksheet.Cell(
                    headerRow,
                    column + 1).Value =
                    headers[column];
            }

            var headerRange =
                worksheet.Range(
                    headerRow,
                    1,
                    headerRow,
                    headers.Length);

            headerRange.Style.Font.Bold = true;

            headerRange.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            headerRange.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            headerRange.Style.Alignment.WrapText = true;

            headerRange.Style.Border.TopBorder =
                XLBorderStyleValues.Thin;

            headerRange.Style.Border.BottomBorder =
                XLBorderStyleValues.Thin;

            headerRange.Style.Border.LeftBorder =
                XLBorderStyleValues.Thin;

            headerRange.Style.Border.RightBorder =
                XLBorderStyleValues.Thin;

            worksheet.Row(headerRow).Height = 24;
        }

        // Veri tablosunun dış ve iç kenarlıklarını düzenler
        private static void StyleDataTable(
            IXLWorksheet worksheet,
            int headerRow,
            int lastRow,
            int columnCount)
        {
            if (lastRow <= headerRow)
                return;

            var tableRange =
                worksheet.Range(
                    headerRow,
                    1,
                    lastRow,
                    columnCount);

            tableRange.Style.Border.OutsideBorder =
                XLBorderStyleValues.Thin;

            tableRange.Style.Border.InsideBorder =
                XLBorderStyleValues.Hair;

            tableRange.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;
        }

        // Tablonun en altındaki toplam satırının görünümünü düzenler
        private static void StyleTotalRow(
            IXLWorksheet worksheet,
            int totalRow,
            int columnCount)
        {
            var totalRange =
                worksheet.Range(
                    totalRow,
                    1,
                    totalRow,
                    columnCount);

            totalRange.Style.Font.Bold = true;

            totalRange.Style.Border.TopBorder =
                XLBorderStyleValues.Medium;

            totalRange.Style.Border.BottomBorder =
                XLBorderStyleValues.Thin;

            totalRange.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            worksheet.Row(totalRow).Height = 22;
        }

        // Excel sayfasının ortak yazdırma ve görünüm ayarlarını yapar
        private static void ApplyWorksheetSettings(
            IXLWorksheet worksheet,
            int columnCount,
            int lastRow)
        {
            // Kullanılan hücrelerde dikey hizalamayı ortalar
            worksheet.Range(
                    1,
                    1,
                    lastRow,
                    columnCount)
                .Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            // Tablo dışındaki gereksiz ızgara çizgilerini gizler
            worksheet.ShowGridLines = false;

            // Yazdırmada raporu tek sayfa genişliğine sığdırır
            worksheet.PageSetup.PagesWide = 1;
            worksheet.PageSetup.PagesTall = 0;

            // Sayfa kenar boşluklarını düzenler
            worksheet.PageSetup.Margins.Top = 0.5;
            worksheet.PageSetup.Margins.Bottom = 0.5;
            worksheet.PageSetup.Margins.Left = 0.4;
            worksheet.PageSetup.Margins.Right = 0.4;

            // Yazdırma alanını yalnızca raporun bulunduğu bölümle sınırlar
            worksheet.PageSetup.PrintAreas.Clear();

            string lastColumnLetter =
                XLHelper.GetColumnLetterFromNumber(columnCount);

            worksheet.PageSetup.PrintAreas.Add(
                $"A1:{lastColumnLetter}{lastRow}");

            // Üst başlık satırlarına biraz daha yükseklik verir
            worksheet.Row(1).Height = 26;
            worksheet.Row(2).Height = 22;
            worksheet.Row(4).Height = 20;
            worksheet.Row(5).Height = 20;
        }

        // İş emri durumunun Türkçe karşılığını döndürür
        private static string GetStatusText(
            WorkOrderStatus status)
        {
            return status switch
            {
                WorkOrderStatus.PendingApproval => "Onay Bekliyor",
                WorkOrderStatus.Approved => "Onaylandı",
                WorkOrderStatus.InWorkshop => "Atölyede",
                WorkOrderStatus.Completed => "Tamamlandı",
                WorkOrderStatus.Rejected => "Reddedildi",
                WorkOrderStatus.Cancelled => "İptal",
                _ => status.ToString()
            };
        }

        // Hücre değerini veri tipini koruyarak Excel'e yazar
        private static void SetCellValue(
            IXLCell cell,
            object? value)
        {
            switch (value)
            {
                case int number:
                    cell.Value = number;
                    break;

                case long longNumber:
                    cell.Value = longNumber;
                    break;

                case decimal amount:
                    cell.Value = amount;
                    break;

                case double doubleNumber:
                    cell.Value = doubleNumber;
                    break;

                case DateTime date:
                    cell.Value = date;
                    cell.Style.NumberFormat.Format = DateFormat;
                    break;

                default:
                    cell.Value = value?.ToString() ?? string.Empty;
                    break;
            }
        }

        // Hazırlanan Excel çalışma kitabını byte dizisine dönüştürür
        private static byte[] SaveWorkbook(
            XLWorkbook workbook)
        {
            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            return stream.ToArray();
        }
    }
}