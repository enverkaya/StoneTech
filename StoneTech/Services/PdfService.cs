using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StoneTech.Enums;
using StoneTech.Models.Report;

namespace StoneTech.Services
{
    public class PdfService : IPdfService
    {
        // İş emri raporunu PDF dosyası olarak oluşturur
        public byte[] CreateWorkOrderReport(WorkOrderReportViewModel model)
        {
            // Filtrede tarih seçilmemişse rapordaki
            // ilk ve son iş emri tarihleri kullanılır
            DateTime? reportStartDate = model.StartDate;
            DateTime? reportEndDate = model.EndDate;

            if (model.WorkOrders.Any())
            {
                reportStartDate ??=
                    model.WorkOrders.Min(x => x.CreatedDate).Date;

                reportEndDate ??=
                    model.WorkOrders.Max(x => x.CreatedDate).Date;
            }

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    // Sekiz sütunun rahat görünmesi için A4 yatay kullanılır
                    page.Size(PageSizes.A4.Landscape());

                    page.Margin(25);

                    page.DefaultTextStyle(x =>
                        x.FontSize(9));

                    // Sayfa başlığı
                    page.Header()
                        .Column(column =>
                        {
                            column.Spacing(4);

                            column.Item()
                                .AlignCenter()
                                .Text("TECHSTONE")
                                .Bold()
                                .FontSize(18);

                            column.Item()
                                .AlignCenter()
                                .Text("İş Emri Raporu")
                                .Bold()
                                .FontSize(13);
                        });

                    // Sayfa içeriği
                    page.Content()
                        .PaddingTop(15)
                        .Column(column =>
                        {
                            column.Spacing(12);

                            // Rapor tarih bilgilerini oluşturur
                            column.Item()
                                .Row(row =>
                                {
                                    row.RelativeItem()
                                        .Text(text =>
                                        {
                                            text.Span("Başlangıç Tarihi: ")
                                                .Bold();

                                            text.Span(
                                                reportStartDate?
                                                    .ToString("dd.MM.yyyy")
                                                ?? "Tüm Zamanlar");
                                        });

                                    row.RelativeItem()
                                        .AlignCenter()
                                        .Text(text =>
                                        {
                                            text.Span("Bitiş Tarihi: ")
                                                .Bold();

                                            text.Span(
                                                reportEndDate?
                                                    .ToString("dd.MM.yyyy")
                                                ?? "Tüm Zamanlar");
                                        });

                                    row.RelativeItem()
                                        .AlignRight()
                                        .Text(text =>
                                        {
                                            text.Span("Rapor Tarihi: ")
                                                .Bold();

                                            text.Span(
                                                DateTime.Now
                                                    .ToString("dd.MM.yyyy HH:mm"));
                                        });
                                });

                            // İş emri raporunun özet bilgilerini gösterir
                            column.Item()
                                .Row(row =>
                                {
                                    row.RelativeItem()
                                        .PaddingRight(4)
                                        .Element(SummaryCellStyle)
                                        .Column(box =>
                                        {
                                            box.Item()
                                                .AlignCenter()
                                                .Text("Toplam İş Emri")
                                                .Bold()
                                                .FontSize(8);

                                            box.Item()
                                                .AlignCenter()
                                                .Text(model.TotalWorkOrderCount.ToString())
                                                .Bold()
                                                .FontSize(10);
                                        });

                                    row.RelativeItem()
                                        .PaddingHorizontal(4)
                                        .Element(SummaryCellStyle)
                                        .Column(box =>
                                        {
                                            box.Item()
                                                .AlignCenter()
                                                .Text("Toplam Tutar")
                                                .Bold()
                                                .FontSize(8);

                                            box.Item()
                                                .AlignCenter()
                                                .Text($"{model.TotalWorkOrderAmount:N2} TL")
                                                .Bold()
                                                .FontSize(10);
                                        });

                                    row.RelativeItem()
                                        .PaddingHorizontal(4)
                                        .Element(SummaryCellStyle)
                                        .Column(box =>
                                        {
                                            box.Item()
                                                .AlignCenter()
                                                .Text("Tahsil Edilen")
                                                .Bold()
                                                .FontSize(8);

                                            box.Item()
                                                .AlignCenter()
                                                .Text($"{model.TotalPaidAmount:N2} TL")
                                                .Bold()
                                                .FontSize(10);
                                        });

                                    row.RelativeItem()
                                        .PaddingLeft(4)
                                        .Element(SummaryCellStyle)
                                        .Column(box =>
                                        {
                                            box.Item()
                                                .AlignCenter()
                                                .Text("Kalan Alacak")
                                                .Bold()
                                                .FontSize(8);

                                            box.Item()
                                                .AlignCenter()
                                                .Text($"{model.TotalRemainingAmount:N2} TL")
                                                .Bold()
                                                .FontSize(10);
                                        });
                                });

                            // İş emri tablosu
                            column.Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(1.5f);
                                        columns.RelativeColumn(2.2f);
                                        columns.RelativeColumn(1.2f);
                                        columns.RelativeColumn(1.3f);
                                        columns.RelativeColumn(0.7f);
                                        columns.RelativeColumn(1.0f);
                                        columns.RelativeColumn(1.3f);
                                        columns.RelativeColumn(1.3f);
                                    });

                                    // Tablo başlıkları
                                    table.Header(header =>
                                    {
                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("İş Emri No")
                                            .Bold();

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Müşteri")
                                            .Bold();

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Tarih")
                                            .Bold();

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Durum")
                                            .Bold();

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Plaka")
                                            .Bold();

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Kesim")
                                            .Bold();

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Toplam")
                                            .Bold();

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Ödenen")
                                            .Bold();
                                    });

                                    // İş emri kayıtlarını tabloya yazar
                                    foreach (var item in model.WorkOrders)
                                    {
                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .Text(item.WorkOrderNumber);

                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .Text(item.CustomerName);

                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .Text(
                                                item.CreatedDate
                                                    .ToString("dd.MM.yyyy"));

                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .Text(
                                                GetStatusText(item.Status));

                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .AlignRight()
                                            .Text(
                                                item.TotalStoneQuantity
                                                    .ToString());

                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .AlignRight()
                                            .Text(
                                                $"{item.CuttingMeter:N2} m");

                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .AlignRight()
                                            .Text(
                                                $"{item.TotalAmount:N2} TL");

                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .AlignRight()
                                            .Text(
                                                $"{item.TotalPaid:N2} TL");
                                    }

                                    // İş emri tablosunun toplam satırını oluşturur
                                    table.Cell()
                                        .ColumnSpan(4)
                                        .Element(TotalCellStyle)
                                        .Text("TOPLAM")
                                        .Bold();

                                    table.Cell()
                                        .Element(TotalCellStyle)
                                        .AlignRight()
                                        .Text(
                                            model.WorkOrders
                                                .Sum(x => x.TotalStoneQuantity)
                                                .ToString())
                                        .Bold();

                                    table.Cell()
                                        .Element(TotalCellStyle)
                                        .AlignRight()
                                        .Text(
                                            $"{model.WorkOrders.Sum(x => x.CuttingMeter):N2} m")
                                        .Bold();

                                    table.Cell()
                                        .Element(TotalCellStyle)
                                        .AlignRight()
                                        .Text(
                                            $"{model.TotalWorkOrderAmount:N2} TL")
                                        .Bold();

                                    table.Cell()
                                        .Element(TotalCellStyle)
                                        .AlignRight()
                                        .Text(
                                            $"{model.TotalPaidAmount:N2} TL")
                                        .Bold();
                                });
                        });

                    // Sayfa numarası
                    CreateFooter(page);
                });
            });

            return document.GeneratePdf();
        }

        // Finans raporunu PDF dosyası olarak oluşturur
        public byte[] CreateFinanceReport(FinanceReportViewModel model)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // Sayfa başlığı
                    CreateReportHeader(
                        page,
                        "Finans Raporu");

                    page.Content()
                        .PaddingTop(15)
                        .Column(column =>
                        {
                            column.Spacing(12);

                            // Rapor tarih bilgilerini gösterir
                            CreateDateInformation(
                                column,
                                model.StartDate,
                                model.EndDate);

                            // Finans raporunun özet bilgilerini gösterir
                            column.Item()
                                .Row(row =>
                                {
                                    row.RelativeItem()
                                        .PaddingRight(4)
                                        .Element(SummaryCellStyle)
                                        .Column(box =>
                                        {
                                            box.Item()
                                                .AlignCenter()
                                                .Text("Toplam İş Tutarı")
                                                .Bold()
                                                .FontSize(8);

                                            box.Item()
                                                .AlignCenter()
                                                .Text($"{model.TotalWorkOrderAmount:N2} TL")
                                                .Bold()
                                                .FontSize(10);
                                        });

                                    row.RelativeItem()
                                        .PaddingHorizontal(4)
                                        .Element(SummaryCellStyle)
                                        .Column(box =>
                                        {
                                            box.Item()
                                                .AlignCenter()
                                                .Text("Toplam Tahsilat")
                                                .Bold()
                                                .FontSize(8);

                                            box.Item()
                                                .AlignCenter()
                                                .Text($"{model.TotalPaidAmount:N2} TL")
                                                .Bold()
                                                .FontSize(10);
                                        });

                                    row.RelativeItem()
                                        .PaddingHorizontal(4)
                                        .Element(SummaryCellStyle)
                                        .Column(box =>
                                        {
                                            box.Item()
                                                .AlignCenter()
                                                .Text("Kalan Alacak")
                                                .Bold()
                                                .FontSize(8);

                                            box.Item()
                                                .AlignCenter()
                                                .Text($"{model.TotalRemainingAmount:N2} TL")
                                                .Bold()
                                                .FontSize(10);
                                        });

                                    row.RelativeItem()
                                        .PaddingLeft(4)
                                        .Element(SummaryCellStyle)
                                        .Column(box =>
                                        {
                                            box.Item()
                                                .AlignCenter()
                                                .Text("Tahsilat Oranı")
                                                .Bold()
                                                .FontSize(8);

                                            box.Item()
                                                .AlignCenter()
                                                .Text($"%{model.CollectionRate:N2}")
                                                .Bold()
                                                .FontSize(10);
                                        });
                                });

                            // Finans detay tablosunu oluşturur
                            column.Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2.2f);
                                        columns.RelativeColumn(1.0f);
                                        columns.RelativeColumn(1.5f);
                                        columns.RelativeColumn(1.5f);
                                        columns.RelativeColumn(1.5f);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Müşteri")
                                            .Bold();

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("İş Emri")
                                            .Bold();

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Toplam Tutar")
                                            .Bold();

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Ödenen")
                                            .Bold();

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Kalan Alacak")
                                            .Bold();
                                    });

                                    foreach (var item in model.Items)
                                    {
                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .Text(item.CustomerName);

                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .AlignRight()
                                            .Text(item.WorkOrderCount.ToString());

                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .AlignRight()
                                            .Text($"{item.TotalAmount:N2} TL");

                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .AlignRight()
                                            .Text($"{item.TotalPaid:N2} TL");

                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .AlignRight()
                                            .Text($"{item.RemainingAmount:N2} TL");
                                    }

                                    // Finans tablosunun toplam satırını oluşturur
                                    table.Cell()
                                        .Element(TotalCellStyle)
                                        .Text("TOPLAM")
                                        .Bold();

                                    table.Cell()
                                        .Element(TotalCellStyle)
                                        .AlignRight()
                                        .Text(
                                            model.Items
                                                .Sum(x => x.WorkOrderCount)
                                                .ToString())
                                        .Bold();

                                    table.Cell()
                                        .Element(TotalCellStyle)
                                        .AlignRight()
                                        .Text($"{model.TotalWorkOrderAmount:N2} TL")
                                        .Bold();

                                    table.Cell()
                                        .Element(TotalCellStyle)
                                        .AlignRight()
                                        .Text($"{model.TotalPaidAmount:N2} TL")
                                        .Bold();

                                    table.Cell()
                                        .Element(TotalCellStyle)
                                        .AlignRight()
                                        .Text($"{model.TotalRemainingAmount:N2} TL")
                                        .Bold();
                                });
                        });

                    CreateFooter(page);
                });
            });

            return document.GeneratePdf();
        }

        // Stok raporunu PDF dosyası olarak oluşturur
        public byte[] CreateStockReport(StockReportViewModel model)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // Sayfa başlığı
                    CreateReportHeader(
                        page,
                        "Stok Raporu");

                    page.Content()
                        .PaddingTop(15)
                        .Column(column =>
                        {
                            column.Spacing(12);

                            // Rapor tarih bilgilerini gösterir
                            CreateDateInformation(
                                column,
                                model.StartDate,
                                model.EndDate);

                            // Stok raporunun özet bilgilerini gösterir
                            column.Item()
                                .Row(row =>
                                {
                                    row.RelativeItem()
                                        .PaddingRight(3)
                                        .Element(SummaryCellStyle)
                                        .Column(box =>
                                        {
                                            box.Item()
                                                .AlignCenter()
                                                .Text("Taş Çeşidi")
                                                .Bold()
                                                .FontSize(8);

                                            box.Item()
                                                .AlignCenter()
                                                .Text(model.StoneCount.ToString())
                                                .Bold()
                                                .FontSize(10);
                                        });

                                    row.RelativeItem()
                                        .PaddingHorizontal(3)
                                        .Element(SummaryCellStyle)
                                        .Column(box =>
                                        {
                                            box.Item()
                                                .AlignCenter()
                                                .Text("Mevcut Stok")
                                                .Bold()
                                                .FontSize(8);

                                            box.Item()
                                                .AlignCenter()
                                                .Text(model.TotalCurrentStock.ToString())
                                                .Bold()
                                                .FontSize(10);
                                        });

                                    row.RelativeItem()
                                        .PaddingHorizontal(3)
                                        .Element(SummaryCellStyle)
                                        .Column(box =>
                                        {
                                            box.Item()
                                                .AlignCenter()
                                                .Text("Dönem Girişi")
                                                .Bold()
                                                .FontSize(8);

                                            box.Item()
                                                .AlignCenter()
                                                .Text(model.TotalEntries.ToString())
                                                .Bold()
                                                .FontSize(10);
                                        });

                                    row.RelativeItem()
                                        .PaddingHorizontal(3)
                                        .Element(SummaryCellStyle)
                                        .Column(box =>
                                        {
                                            box.Item()
                                                .AlignCenter()
                                                .Text("Dönem Çıkışı")
                                                .Bold()
                                                .FontSize(8);

                                            box.Item()
                                                .AlignCenter()
                                                .Text(model.TotalExits.ToString())
                                                .Bold()
                                                .FontSize(10);
                                        });

                                    row.RelativeItem()
                                        .PaddingLeft(3)
                                        .Element(SummaryCellStyle)
                                        .Column(box =>
                                        {
                                            box.Item()
                                                .AlignCenter()
                                                .Text("İade")
                                                .Bold()
                                                .FontSize(8);

                                            box.Item()
                                                .AlignCenter()
                                                .Text(model.TotalReturns.ToString())
                                                .Bold()
                                                .FontSize(10);
                                        });
                                });

                            // Stok tablosunu Excel raporuyla aynı sade yapıda oluşturur
                            column.Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2.4f);
                                        columns.RelativeColumn(1.6f);
                                        columns.RelativeColumn(1.2f);
                                        columns.RelativeColumn(1.2f);
                                        columns.RelativeColumn(1.2f);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Taş")
                                            .Bold();

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Renk")
                                            .Bold();

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Mevcut Stok")
                                            .Bold();

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Dönem Girişi")
                                            .Bold();

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Dönem Çıkışı")
                                            .Bold();
                                    });

                                    foreach (var item in model.Items)
                                    {
                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .Text(item.StoneName);

                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .Text(
                                                string.IsNullOrWhiteSpace(item.Color)
                                                    ? "-"
                                                    : item.Color);

                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .AlignRight()
                                            .Text(item.CurrentStock.ToString());

                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .AlignRight()
                                            .Text(item.TotalEntry.ToString());

                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .AlignRight()
                                            .Text(item.TotalExit.ToString());
                                    }

                                    // Stok tablosunun toplam satırını oluşturur
                                    table.Cell()
                                        .ColumnSpan(2)
                                        .Element(TotalCellStyle)
                                        .Text("TOPLAM")
                                        .Bold();

                                    table.Cell()
                                        .Element(TotalCellStyle)
                                        .AlignRight()
                                        .Text(model.TotalCurrentStock.ToString())
                                        .Bold();

                                    table.Cell()
                                        .Element(TotalCellStyle)
                                        .AlignRight()
                                        .Text(model.TotalEntries.ToString())
                                        .Bold();

                                    table.Cell()
                                        .Element(TotalCellStyle)
                                        .AlignRight()
                                        .Text(model.TotalExits.ToString())
                                        .Bold();
                                });
                        });

                    CreateFooter(page);
                });
            });

            return document.GeneratePdf();
        }

        // Alım raporunu PDF dosyası olarak oluşturur
        public byte[] CreatePurchaseReport(PurchaseReportViewModel model)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // Sayfa başlığı
                    CreateReportHeader(
                        page,
                        "Alım Raporu");

                    page.Content()
                        .PaddingTop(15)
                        .Column(column =>
                        {
                            column.Spacing(12);

                            // Rapor tarih bilgilerini gösterir
                            CreateDateInformation(
                                column,
                                model.StartDate,
                                model.EndDate);

                            // Alım raporunun özet bilgilerini gösterir
                            column.Item()
                                .Row(row =>
                                {
                                    row.RelativeItem()
                                        .PaddingRight(4)
                                        .Element(SummaryCellStyle)
                                        .Column(box =>
                                        {
                                            box.Item()
                                                .AlignCenter()
                                                .Text("Alım Sayısı")
                                                .Bold()
                                                .FontSize(8);

                                            box.Item()
                                                .AlignCenter()
                                                .Text(model.PurchaseCount.ToString())
                                                .Bold()
                                                .FontSize(10);
                                        });

                                    row.RelativeItem()
                                        .PaddingHorizontal(4)
                                        .Element(SummaryCellStyle)
                                        .Column(box =>
                                        {
                                            box.Item()
                                                .AlignCenter()
                                                .Text("Tedarikçi")
                                                .Bold()
                                                .FontSize(8);

                                            box.Item()
                                                .AlignCenter()
                                                .Text(model.SupplierCount.ToString())
                                                .Bold()
                                                .FontSize(10);
                                        });

                                    row.RelativeItem()
                                        .PaddingHorizontal(4)
                                        .Element(SummaryCellStyle)
                                        .Column(box =>
                                        {
                                            box.Item()
                                                .AlignCenter()
                                                .Text("Toplam Plaka")
                                                .Bold()
                                                .FontSize(8);

                                            box.Item()
                                                .AlignCenter()
                                                .Text(model.TotalPlateCount.ToString())
                                                .Bold()
                                                .FontSize(10);
                                        });

                                    row.RelativeItem()
                                        .PaddingLeft(4)
                                        .Element(SummaryCellStyle)
                                        .Column(box =>
                                        {
                                            box.Item()
                                                .AlignCenter()
                                                .Text("Toplam Alım Tutarı")
                                                .Bold()
                                                .FontSize(8);

                                            box.Item()
                                                .AlignCenter()
                                                .Text($"{model.TotalAmount:N2} TL")
                                                .Bold()
                                                .FontSize(10);
                                        });
                                });

                            // Alım detay tablosunu oluşturur
                            column.Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(1.6f);
                                        columns.RelativeColumn(1.7f);
                                        columns.RelativeColumn(1.1f);
                                        columns.RelativeColumn(0.8f);
                                        columns.RelativeColumn(0.8f);
                                        columns.RelativeColumn(1.4f);
                                        columns.RelativeColumn(2.0f);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Alım No")
                                            .Bold();

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Tedarikçi")
                                            .Bold();

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Tarih")
                                            .Bold();

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Kalem")
                                            .Bold();

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Plaka")
                                            .Bold();

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Toplam Tutar")
                                            .Bold();

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Açıklama")
                                            .Bold();
                                    });

                                    foreach (var item in model.Items)
                                    {
                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .Text(item.PurchaseNumber);

                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .Text(item.SupplierName);

                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .Text(
                                                item.PurchaseDate
                                                    .ToString("dd.MM.yyyy"));

                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .AlignRight()
                                            .Text(item.DetailCount.ToString());

                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .AlignRight()
                                            .Text(item.TotalQuantity.ToString());

                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .AlignRight()
                                            .Text($"{item.TotalAmount:N2} TL");

                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .Text(
                                                string.IsNullOrWhiteSpace(item.Description)
                                                    ? "-"
                                                    : item.Description);
                                    }

                                    // Alım tablosunun toplam satırını oluşturur
                                    table.Cell()
                                        .ColumnSpan(3)
                                        .Element(TotalCellStyle)
                                        .Text("TOPLAM")
                                        .Bold();

                                    table.Cell()
                                        .Element(TotalCellStyle)
                                        .AlignRight()
                                        .Text(
                                            model.Items
                                                .Sum(x => x.DetailCount)
                                                .ToString())
                                        .Bold();

                                    table.Cell()
                                        .Element(TotalCellStyle)
                                        .AlignRight()
                                        .Text(model.TotalPlateCount.ToString())
                                        .Bold();

                                    table.Cell()
                                        .Element(TotalCellStyle)
                                        .AlignRight()
                                        .Text($"{model.TotalAmount:N2} TL")
                                        .Bold();

                                    table.Cell()
                                        .Element(TotalCellStyle)
                                        .Text(string.Empty);
                                });
                        });

                    CreateFooter(page);
                });
            });

            return document.GeneratePdf();
        }

        // Müşteri raporunu PDF dosyası olarak oluşturur
        public byte[] CreateCustomerReport(CustomerReportViewModel model)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // Sayfa başlığı
                    CreateReportHeader(
                        page,
                        "Müşteri Raporu");

                    page.Content()
                        .PaddingTop(15)
                        .Column(column =>
                        {
                            column.Spacing(12);

                            // Rapor tarih bilgilerini gösterir
                            CreateDateInformation(
                                column,
                                model.StartDate,
                                model.EndDate);

                            // Müşteri raporunun özet bilgilerini gösterir
                            column.Item()
                                .Row(row =>
                                {
                                    row.RelativeItem()
                                        .PaddingRight(3)
                                        .Element(SummaryCellStyle)
                                        .Column(box =>
                                        {
                                            box.Item()
                                                .AlignCenter()
                                                .Text("Müşteri")
                                                .Bold()
                                                .FontSize(8);

                                            box.Item()
                                                .AlignCenter()
                                                .Text(model.CustomerCount.ToString())
                                                .Bold()
                                                .FontSize(10);
                                        });

                                    row.RelativeItem()
                                        .PaddingHorizontal(3)
                                        .Element(SummaryCellStyle)
                                        .Column(box =>
                                        {
                                            box.Item()
                                                .AlignCenter()
                                                .Text("İş Emri")
                                                .Bold()
                                                .FontSize(8);

                                            box.Item()
                                                .AlignCenter()
                                                .Text(model.WorkOrderCount.ToString())
                                                .Bold()
                                                .FontSize(10);
                                        });

                                    row.RelativeItem()
                                        .PaddingHorizontal(3)
                                        .Element(SummaryCellStyle)
                                        .Column(box =>
                                        {
                                            box.Item()
                                                .AlignCenter()
                                                .Text("İş Hacmi")
                                                .Bold()
                                                .FontSize(8);

                                            box.Item()
                                                .AlignCenter()
                                                .Text($"{model.TotalBusinessVolume:N2} TL")
                                                .Bold()
                                                .FontSize(10);
                                        });

                                    row.RelativeItem()
                                        .PaddingHorizontal(3)
                                        .Element(SummaryCellStyle)
                                        .Column(box =>
                                        {
                                            box.Item()
                                                .AlignCenter()
                                                .Text("Toplam Ödeme")
                                                .Bold()
                                                .FontSize(8);

                                            box.Item()
                                                .AlignCenter()
                                                .Text($"{model.TotalPaid:N2} TL")
                                                .Bold()
                                                .FontSize(10);
                                        });

                                    row.RelativeItem()
                                        .PaddingLeft(3)
                                        .Element(SummaryCellStyle)
                                        .Column(box =>
                                        {
                                            box.Item()
                                                .AlignCenter()
                                                .Text("Kalan Alacak")
                                                .Bold()
                                                .FontSize(8);

                                            box.Item()
                                                .AlignCenter()
                                                .Text($"{model.TotalRemaining:N2} TL")
                                                .Bold()
                                                .FontSize(10);
                                        });
                                });

                            // Müşteri detay tablosunu oluşturur
                            column.Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2.0f);
                                        columns.RelativeColumn(1.1f);
                                        columns.RelativeColumn(1.5f);
                                        columns.RelativeColumn(0.8f);
                                        columns.RelativeColumn(1.4f);
                                        columns.RelativeColumn(1.4f);
                                        columns.RelativeColumn(1.4f);
                                        columns.RelativeColumn(0.9f);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Müşteri")
                                            .Bold();

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Tip")
                                            .Bold();

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Telefon")
                                            .Bold();

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("İş Emri")
                                            .Bold();

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("İş Hacmi")
                                            .Bold();

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Ödeme")
                                            .Bold();

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Kalan")
                                            .Bold();

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Durum")
                                            .Bold();
                                    });

                                    foreach (var item in model.Items)
                                    {
                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .Text(item.CustomerName);

                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .Text(
                                                item.CustomerType ==
                                                CustomerType.Individual
                                                    ? "Bireysel"
                                                    : "Kurumsal");

                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .Text(item.Phone);

                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .AlignRight()
                                            .Text(item.WorkOrderCount.ToString());

                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .AlignRight()
                                            .Text($"{item.TotalAmount:N2} TL");

                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .AlignRight()
                                            .Text($"{item.TotalPaid:N2} TL");

                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .AlignRight()
                                            .Text($"{item.RemainingAmount:N2} TL");

                                        table.Cell()
                                            .Element(BodyCellStyle)
                                            .Text(
                                                item.IsActive
                                                    ? "Aktif"
                                                    : "Pasif");
                                    }

                                    // Müşteri tablosunun toplam satırını oluşturur
                                    table.Cell()
                                        .ColumnSpan(3)
                                        .Element(TotalCellStyle)
                                        .Text("TOPLAM")
                                        .Bold();

                                    table.Cell()
                                        .Element(TotalCellStyle)
                                        .AlignRight()
                                        .Text(model.WorkOrderCount.ToString())
                                        .Bold();

                                    table.Cell()
                                        .Element(TotalCellStyle)
                                        .AlignRight()
                                        .Text($"{model.TotalBusinessVolume:N2} TL")
                                        .Bold();

                                    table.Cell()
                                        .Element(TotalCellStyle)
                                        .AlignRight()
                                        .Text($"{model.TotalPaid:N2} TL")
                                        .Bold();

                                    table.Cell()
                                        .Element(TotalCellStyle)
                                        .AlignRight()
                                        .Text($"{model.TotalRemaining:N2} TL")
                                        .Bold();

                                    table.Cell()
                                        .Element(TotalCellStyle)
                                        .Text(string.Empty);
                                });
                        });

                    CreateFooter(page);
                });
            });

            return document.GeneratePdf();
        }

        // TECHSTONE ve rapor adı alanını oluşturur
        private static void CreateReportHeader(
            PageDescriptor page,
            string reportTitle)
        {
            page.Header()
                .Column(column =>
                {
                    column.Spacing(4);

                    column.Item()
                        .AlignCenter()
                        .Text("TECHSTONE")
                        .Bold()
                        .FontSize(18);

                    column.Item()
                        .AlignCenter()
                        .Text(reportTitle)
                        .Bold()
                        .FontSize(13);
                });
        }

        // Raporun başlangıç, bitiş ve oluşturulma tarihlerini gösterir
        private static void CreateDateInformation(
            ColumnDescriptor column,
            DateTime? startDate,
            DateTime? endDate)
        {
            column.Item()
                .Row(row =>
                {
                    row.RelativeItem()
                        .Text(text =>
                        {
                            text.Span("Başlangıç Tarihi: ")
                                .Bold();

                            text.Span(
                                startDate?.ToString("dd.MM.yyyy")
                                ?? "Tüm Zamanlar");
                        });

                    row.RelativeItem()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("Bitiş Tarihi: ")
                                .Bold();

                            text.Span(
                                endDate?.ToString("dd.MM.yyyy")
                                ?? "Tüm Zamanlar");
                        });

                    row.RelativeItem()
                        .AlignRight()
                        .Text(text =>
                        {
                            text.Span("Rapor Tarihi: ")
                                .Bold();

                            text.Span(
                                DateTime.Now
                                    .ToString("dd.MM.yyyy HH:mm"));
                        });
                });
        }

        // Özet bilgi kutularının görünümünü ayarlar
        private static IContainer SummaryCellStyle(
            IContainer container)
        {
            return container
                .Border(0.7f)
                .PaddingVertical(7)
                .PaddingHorizontal(5)
                .AlignMiddle();
        }

        // Tablo başlık hücrelerinin görünümünü ayarlar
        private static IContainer HeaderCellStyle(
            IContainer container)
        {
            return container
                .Border(1)
                .PaddingVertical(6)
                .PaddingHorizontal(4)
                .AlignCenter()
                .AlignMiddle();
        }

        // Tablo veri hücrelerinin görünümünü ayarlar
        private static IContainer BodyCellStyle(
            IContainer container)
        {
            return container
                .BorderBottom(0.5f)
                .BorderLeft(0.5f)
                .BorderRight(0.5f)
                .PaddingVertical(5)
                .PaddingHorizontal(4)
                .AlignMiddle();
        }

        // Toplam satırı hücrelerinin görünümünü ayarlar
        private static IContainer TotalCellStyle(
            IContainer container)
        {
            return container
                .BorderTop(1)
                .BorderBottom(1)
                .BorderLeft(0.5f)
                .BorderRight(0.5f)
                .PaddingVertical(6)
                .PaddingHorizontal(4)
                .AlignMiddle();
        }

        // Sayfanın altındaki sayfa numarası alanını oluşturur
        private static void CreateFooter(
            PageDescriptor page)
        {
            page.Footer()
                .AlignCenter()
                .Text(text =>
                {
                    text.Span("Sayfa ");

                    text.CurrentPageNumber();

                    text.Span(" / ");

                    text.TotalPages();
                });
        }

        // İş emri durumunun Türkçe karşılığını döndürür
        private static string GetStatusText(
            WorkOrderStatus status)
        {
            return status switch
            {
                WorkOrderStatus.PendingApproval =>
                    "Onay Bekliyor",

                WorkOrderStatus.Approved =>
                    "Onaylandı",

                WorkOrderStatus.InWorkshop =>
                    "Atölyede",

                WorkOrderStatus.Completed =>
                    "Tamamlandı",

                WorkOrderStatus.Rejected =>
                    "Reddedildi",

                WorkOrderStatus.Cancelled =>
                    "İptal",

                _ => status.ToString()
            };
        }
    }
}