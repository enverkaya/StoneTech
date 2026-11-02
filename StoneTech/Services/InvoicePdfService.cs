using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StoneTech.Enums;
using StoneTech.Models.Invoice;

namespace StoneTech.Services
{
    public class InvoicePdfService : IInvoicePdfService
    {
        public byte[] CreateInvoicePdf(InvoiceDetailsViewModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (model.Company == null)
                throw new InvalidOperationException(
                    "Fatura PDF'i oluşturulabilmesi için firma bilgileri gereklidir.");

            if (model.Status != InvoiceStatus.Issued)
                throw new InvalidOperationException(
                    "Yalnızca kesinleştirilmiş faturalar PDF olarak oluşturulabilir.");

            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    // A4 fatura sayfası oluşturulur
                    page.Size(PageSizes.A4);

                    // Sayfanın dış boşlukları belirlenir
                    page.Margin(30);

                    // PDF genelinde kullanılacak yazı tipi ve boyutu belirlenir
                    page.DefaultTextStyle(x =>
                        x.FontSize(9)
                         .FontFamily("Lato"));


                    // Faturanın üst bölümü oluşturulur
                    page.Header()
                        .Element(container =>
                            CreateHeader(container, model));

                    // Faturanın ana içeriği oluşturulur
                    page.Content()
                        .PaddingTop(20)
                        .Element(container =>
                            CreateContent(container, model));

                    // Sayfa alt bilgisi oluşturulur
                    page.Footer()
                        .Element(CreateFooter);
                });
            }).GeneratePdf();
        }

        // Faturanın üst bölümünü oluşturur
        // Sol tarafta satıcı firma, sağ tarafta fatura bilgileri bulunur
        private static void CreateHeader(
            IContainer container,
            InvoiceDetailsViewModel model)
        {
            var company = model.Company!;

            container.Column(column =>
            {
                column.Spacing(15);

                column.Item()
                    .Row(row =>
                    {
                        // Sol tarafta firma logosu ve firma bilgileri gösterilir
                        row.RelativeItem(3)
                            .Column(left =>
                            {
                                // Logo dosyası mevcutsa faturada gösterilir
                                var logoPath = GetLogoPhysicalPath(
                                    company.LogoUrl);

                                if (!string.IsNullOrWhiteSpace(logoPath))
                                {
                                    left.Item()
                                        .Height(55)
                                        .AlignLeft()
                                        .Image(logoPath)
                                        .FitArea();

                                    left.Item()
                                        .PaddingTop(8);
                                }

                                left.Item()
                                    .Text(company.CompanyName)
                                    .FontSize(15)
                                    .Bold();

                                left.Item()
                                    .PaddingTop(4)
                                    .Text(company.Address)
                                    .FontSize(8);

                                if (!string.IsNullOrWhiteSpace(company.Phone))
                                {
                                    left.Item()
                                        .PaddingTop(2)
                                        .Text($"Telefon: {company.Phone}")
                                        .FontSize(8);
                                }

                                if (!string.IsNullOrWhiteSpace(company.Email))
                                {
                                    left.Item()
                                        .PaddingTop(2)
                                        .Text($"E-posta: {company.Email}")
                                        .FontSize(8);
                                }

                                if (!string.IsNullOrWhiteSpace(company.Website))
                                {
                                    left.Item()
                                        .PaddingTop(2)
                                        .Text($"Web: {company.Website}")
                                        .FontSize(8);
                                }

                                left.Item()
                                    .PaddingTop(5)
                                    .Text(
                                        $"Vergi Dairesi: {company.TaxOffice}")
                                    .FontSize(8);

                                left.Item()
                                    .Text(
                                        $"Vergi No: {company.TaxNumber}")
                                    .FontSize(8);
                            });

                        row.ConstantItem(20);

                        // Sağ tarafta belge başlığı ve fatura bilgileri gösterilir
                        row.RelativeItem(2)
                            .Border(1)
                            .BorderColor(Colors.Grey.Lighten2)
                            .Padding(15)
                            .Column(right =>
                            {
                                right.Item()
                                    .AlignCenter()
                                    .Text("FATURA")
                                    .FontSize(22)
                                    .Bold();

                                right.Item()
                                    .PaddingTop(12)
                                    .Element(x =>
                                        CreateInformationRow(
                                            x,
                                            "Fatura No",
                                            model.InvoiceNumber));

                                right.Item()
                                    .PaddingTop(6)
                                    .Element(x =>
                                        CreateInformationRow(
                                            x,
                                            "Fatura Tarihi",
                                            model.InvoiceDate
                                                .ToString("dd.MM.yyyy")));

                                right.Item()
                                    .PaddingTop(6)
                                    .Element(x =>
                                        CreateInformationRow(
                                            x,
                                            "İş Emri",
                                            model.WorkOrderNumber));
                            });
                    });

                // Üst bölümün altına ayırıcı çizgi eklenir
                column.Item()
                    .LineHorizontal(1)
                    .LineColor(Colors.Grey.Lighten2);
            });
        }

        // Faturanın ana içeriğini oluşturur
        private static void CreateContent(
            IContainer container,
            InvoiceDetailsViewModel model)
        {
            container.Column(column =>
            {
                column.Spacing(18);

                // Müşteri bilgileri oluşturulur
                column.Item()
                    .Element(x =>
                        CreateCustomerInformation(x, model));

                // Fatura kalemleri oluşturulur
                column.Item()
                    .Element(x =>
                        CreateInvoiceTable(x, model));

                // Fatura toplamları oluşturulur
                column.Item()
                    .AlignRight()
                    .Width(260)
                    .Element(x =>
                        CreateTotals(x, model));

                // Fatura açıklaması varsa gösterilir
                if (!string.IsNullOrWhiteSpace(model.Description))
                {
                    column.Item()
                        .Element(x =>
                            CreateDescription(
                                x,
                                model.Description));
                }
            });
        }

        // Müşteri / alıcı bilgilerini oluşturur
        private static void CreateCustomerInformation(
            IContainer container,
            InvoiceDetailsViewModel model)
        {
            container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Column(column =>
                {
                    column.Item()
                        .Background(Colors.Grey.Lighten3)
                        .PaddingVertical(7)
                        .PaddingHorizontal(10)
                        .Text("ALICI BİLGİLERİ")
                        .Bold()
                        .FontSize(10);

                    column.Item()
                        .Padding(10)
                        .Column(info =>
                        {
                            info.Spacing(4);

                            info.Item()
                                .Text(model.CustomerName)
                                .Bold()
                                .FontSize(11);

                            if (model.CustomerType ==
                                CustomerType.Corporate)
                            {
                                if (!string.IsNullOrWhiteSpace(
                                        model.TaxOffice))
                                {
                                    info.Item()
                                        .Text(
                                            $"Vergi Dairesi: {model.TaxOffice}");
                                }

                                if (!string.IsNullOrWhiteSpace(
                                        model.TaxNumber))
                                {
                                    info.Item()
                                        .Text(
                                            $"Vergi No: {model.TaxNumber}");
                                }
                            }
                            else
                            {
                                if (!string.IsNullOrWhiteSpace(
                                        model.IdentityNumber))
                                {
                                    info.Item()
                                        .Text(
                                            $"T.C. Kimlik No: {model.IdentityNumber}");
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(model.Address))
                            {
                                info.Item()
                                    .Text($"Adres: {model.Address}");
                            }
                        });
                });
        }

        // Faturadaki ürün ve hizmet kalemlerini tablo olarak oluşturur
        private static void CreateInvoiceTable(
            IContainer container,
            InvoiceDetailsViewModel model)
        {
            container.Table(table =>
            {
                // Tablo sütun genişlikleri belirlenir
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(28);
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1.4f);
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(1.6f);
                });

                // Tablo başlıkları oluşturulur
                table.Header(header =>
                {
                    header.Cell()
                        .Element(HeaderCellStyle)
                        .AlignCenter()
                        .Text("#");

                    header.Cell()
                        .Element(HeaderCellStyle)
                        .Text("Açıklama");

                    header.Cell()
                        .Element(HeaderCellStyle)
                        .AlignCenter()
                        .Text("Miktar");

                    header.Cell()
                        .Element(HeaderCellStyle)
                        .AlignCenter()
                        .Text("Birim");

                    header.Cell()
                        .Element(HeaderCellStyle)
                        .AlignRight()
                        .Text("Birim Fiyat");

                    header.Cell()
                        .Element(HeaderCellStyle)
                        .AlignRight()
                        .Text("KDV");

                    header.Cell()
                        .Element(HeaderCellStyle)
                        .AlignRight()
                        .Text("Toplam");
                });

                // Fatura kalemleri tabloya eklenir
                for (var index = 0;
                     index < model.Details.Count;
                     index++)
                {
                    var detail = model.Details[index];

                    table.Cell()
                        .Element(BodyCellStyle)
                        .AlignCenter()
                        .Text((index + 1).ToString());

                    table.Cell()
                        .Element(BodyCellStyle)
                        .Text(detail.Description);

                    table.Cell()
                        .Element(BodyCellStyle)
                        .AlignCenter()
                        .Text(FormatQuantity(detail.Quantity));

                    table.Cell()
                        .Element(BodyCellStyle)
                        .AlignCenter()
                        .Text(detail.Unit);

                    table.Cell()
                        .Element(BodyCellStyle)
                        .AlignRight()
                        .Text(FormatMoney(detail.UnitPrice));

                    table.Cell()
                        .Element(BodyCellStyle)
                        .AlignRight()
                        .Column(cell =>
                        {
                            cell.Item()
                                .Text($"%{FormatRate(detail.VatRate)}");

                            cell.Item()
                                .Text(FormatMoney(detail.VatAmount))
                                .FontSize(7)
                                .FontColor(Colors.Grey.Darken1);
                        });

                    table.Cell()
                        .Element(BodyCellStyle)
                        .AlignRight()
                        .Text(FormatMoney(detail.TotalAmount))
                        .Bold();
                }
            });
        }

        // Fatura ara toplam, KDV ve genel toplam bölümünü oluşturur
        private static void CreateTotals(
            IContainer container,
            InvoiceDetailsViewModel model)
        {
            container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Column(column =>
                {
                    column.Item()
                        .Padding(9)
                        .Row(row =>
                        {
                            row.RelativeItem()
                                .Text("Ara Toplam");

                            row.ConstantItem(110)
                                .AlignRight()
                                .Text(FormatMoney(model.SubTotal));
                        });

                    column.Item()
                        .LineHorizontal(1)
                        .LineColor(Colors.Grey.Lighten3);

                    column.Item()
                        .Padding(9)
                        .Row(row =>
                        {
                            row.RelativeItem()
                                .Text(
                                    $"KDV (%{FormatRate(model.VatRate)})");

                            row.ConstantItem(110)
                                .AlignRight()
                                .Text(FormatMoney(model.VatAmount));
                        });

                    column.Item()
                        .LineHorizontal(1)
                        .LineColor(Colors.Grey.Lighten2);

                    column.Item()
                        .Background(Colors.Grey.Lighten3)
                        .Padding(10)
                        .Row(row =>
                        {
                            row.RelativeItem()
                                .Text("GENEL TOPLAM")
                                .Bold()
                                .FontSize(11);

                            row.ConstantItem(110)
                                .AlignRight()
                                .Text(FormatMoney(model.TotalAmount))
                                .Bold()
                                .FontSize(12);
                        });
                });
        }

        // Fatura açıklama alanını oluşturur
        private static void CreateDescription(
            IContainer container,
            string description)
        {
            container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Column(column =>
                {
                    column.Item()
                        .Background(Colors.Grey.Lighten3)
                        .PaddingVertical(6)
                        .PaddingHorizontal(10)
                        .Text("AÇIKLAMA")
                        .Bold()
                        .FontSize(9);

                    column.Item()
                        .Padding(10)
                        .Text(description)
                        .FontSize(8);
                });
        }

        // Fatura bilgi kutusunda etiket ve değeri aynı satırda gösterir
        private static void CreateInformationRow(
            IContainer container,
            string label,
            string value)
        {
            container.Row(row =>
            {
                row.RelativeItem()
                    .Text(label)
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken1);

                row.RelativeItem()
                    .AlignRight()
                    .Text(value)
                    .Bold()
                    .FontSize(8);
            });
        }

        // Fatura tablosunun başlık hücresi görünümünü oluşturur
        private static IContainer HeaderCellStyle(
            IContainer container)
        {
            return container
                .Background(Colors.Grey.Darken3)
                .Border(0.5f)
                .BorderColor(Colors.Grey.Darken2)
                .PaddingVertical(7)
                .PaddingHorizontal(5)
                .DefaultTextStyle(x =>
                    x.FontColor(Colors.White)
                     .Bold()
                     .FontSize(8));
        }

        // Fatura tablosunun veri hücresi görünümünü oluşturur
        private static IContainer BodyCellStyle(
            IContainer container)
        {
            return container
                .BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Lighten2)
                .PaddingVertical(7)
                .PaddingHorizontal(5)
                .DefaultTextStyle(x =>
                    x.FontSize(8));
        }

        // PDF sayfasının alt bilgi alanını oluşturur
        private static void CreateFooter(
            IContainer container)
        {
            container
                .BorderTop(1)
                .BorderColor(Colors.Grey.Lighten2)
                .PaddingTop(8)
                .Row(row =>
                {
                    row.RelativeItem()
                        .Text(
                            "Bu belge StoneTech sistemi üzerinden oluşturulmuştur.")
                        .FontSize(7)
                        .FontColor(Colors.Grey.Darken1);

                    row.RelativeItem()
                        .AlignRight()
                        .Text(text =>
                        {
                            text.DefaultTextStyle(x =>
                                x.FontSize(7)
                                 .FontColor(
                                     Colors.Grey.Darken1));

                            text.Span("Sayfa ");

                            text.CurrentPageNumber();

                            text.Span(" / ");

                            text.TotalPages();
                        });
                });
        }

        // Para değerlerini Türk Lirası formatında gösterir
        private static string FormatMoney(decimal value)
        {
            return $"{value:N2} TL";
        }

        // KDV oranını gereksiz ondalık sıfırlar olmadan gösterir
        private static string FormatRate(decimal value)
        {
            return value.ToString("0.##");
        }

        // Miktarı gereksiz ondalık sıfırlar olmadan gösterir
        private static string FormatQuantity(decimal value)
        {
            return value.ToString("0.##");
        }

        // Veritabanındaki web logo yolunu fiziksel dosya yoluna dönüştürür
        // Firma logosu ve faturaya özel logo snapshot yollarını destekler
        // Dosya bulunamazsa null döndürülür ve PDF logosuz oluşturulur
        private static string? GetLogoPhysicalPath(
            string? logoUrl)
        {
            if (string.IsNullOrWhiteSpace(logoUrl))
                return null;

            // Güvenlik nedeniyle yalnızca izin verilen logo klasörleri kabul edilir
            const string companyLogoPrefix =
                "/uploads/company/";

            const string invoiceLogoPrefix =
                "/uploads/invoices/logos/";

            bool isAllowedPath =
                logoUrl.StartsWith(
                    companyLogoPrefix,
                    StringComparison.OrdinalIgnoreCase) ||
                logoUrl.StartsWith(
                    invoiceLogoPrefix,
                    StringComparison.OrdinalIgnoreCase);

            if (!isAllowedPath)
                return null;

            // Web yolu fiziksel dosya yoluna dönüştürülür
            var relativePath = logoUrl
                .TrimStart('/')
                .Replace('/', Path.DirectorySeparatorChar);

            var webRootPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot");

            var physicalPath = Path.Combine(
                webRootPath,
                relativePath);

            return File.Exists(physicalPath)
                ? physicalPath
                : null;
        }
    }
}