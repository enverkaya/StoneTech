using Microsoft.EntityFrameworkCore;
using StoneTech.Entities;

namespace StoneTech.Data
{
    public class AppDbContext : DbContext
    {
        // DbContext ayarlarını dışarıdan alır
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // Veritabanı tabloları

        // Taşlar
        public DbSet<Stone> Stones { get; set; }

        // Stok hareketleri
        public DbSet<StockMovement> StockMovements { get; set; }

        // Müşteriler
        public DbSet<Customer> Customers { get; set; }

        // İş emirleri
        public DbSet<WorkOrder> WorkOrders { get; set; }

        // İş emri detayları
        public DbSet<WorkOrderDetail> WorkOrderDetails { get; set; }

        // Ödemeler
        public DbSet<Payment> Payments { get; set; }

        // Tedarikçiler
        public DbSet<Supplier> Suppliers { get; set; }

        // Alımlar
        public DbSet<Purchase> Purchases { get; set; }

        // Alım detayları
        public DbSet<PurchaseDetail> PurchaseDetails { get; set; }

        // Personeller
        public DbSet<Employee> Employees { get; set; }

        // Kullanıcılar
        public DbSet<User> Users { get; set; }

        // Sistem işlem geçmişi kayıtları
        public DbSet<ActivityLog> ActivityLogs { get; set; }

        //Faturalar 
        public DbSet<Invoice> Invoices { get; set; }

        //Fatura detayları
        public DbSet<InvoiceDetail> InvoiceDetails { get; set; }

        // Sistemi kullanan işletmenin firma bilgilerini tutar
        public DbSet<CompanySetting> CompanySettings { get; set; }

        // Ana sayfa slider görselleri
        public DbSet<Slider> Sliders { get; set; }

        // Web sitesinde sunulan hizmetler
        public DbSet<Service> Services { get; set; }

        // Hizmetlere ait fotoğraflar
        public DbSet<ServiceImage> ServiceImages { get; set; }

        // Web sitesi galeri görselleri
        public DbSet<Gallery> Galleries { get; set; }

        // Hakkımızda sayfası bilgileri
        public DbSet<About> Abouts { get; set; }

        // Neden bizi tercih etmelisiniz özellikleri
        public DbSet<WhyChooseUs> WhyChooseUsItems { get; set; }


        // Web sitesinden gönderilen iletişim mesajları
        public DbSet<ContactMessage> ContactMessages { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // İLİŞKİLER

            // Bir hizmetin birden fazla görseli olabilir
            modelBuilder.Entity<ServiceImage>()
                .HasOne(x => x.Service)
                .WithMany(x => x.ServiceImages)
                .HasForeignKey(x => x.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Bir taşın birden fazla stok hareketi olabilir
            modelBuilder.Entity<StockMovement>()
                .HasOne(x => x.Stone)
                .WithMany(x => x.StockMovements)
                .HasForeignKey(x => x.StoneId)
                .OnDelete(DeleteBehavior.Restrict);

            // Bir müşterinin birden fazla iş emri olabilir
            modelBuilder.Entity<WorkOrder>()
                .HasOne(x => x.Customer)
                .WithMany(x => x.WorkOrders)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Bir iş emrinin birden fazla detayı olabilir
            modelBuilder.Entity<WorkOrderDetail>()
                .HasOne(x => x.WorkOrder)
                .WithMany(x => x.WorkOrderDetails)
                .HasForeignKey(x => x.WorkOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Bir taş birden fazla iş emri detayında kullanılabilir
            modelBuilder.Entity<WorkOrderDetail>()
                .HasOne(x => x.Stone)
                .WithMany(x => x.WorkOrderDetails)
                .HasForeignKey(x => x.StoneId)
                .OnDelete(DeleteBehavior.Restrict);

            // Bir iş emrinin birden fazla ödemesi olabilir
            modelBuilder.Entity<Payment>()
                .HasOne(x => x.WorkOrder)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.WorkOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Bir tedarikçiden birden fazla alım yapılabilir
            modelBuilder.Entity<Purchase>()
                .HasOne(x => x.Supplier)
                .WithMany(x => x.Purchases)
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            // Bir alımın birden fazla detayı olabilir
            modelBuilder.Entity<PurchaseDetail>()
                .HasOne(x => x.Purchase)
                .WithMany(x => x.PurchaseDetails)
                .HasForeignKey(x => x.PurchaseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Bir taş birden fazla alım detayında bulunabilir
            modelBuilder.Entity<PurchaseDetail>()
                .HasOne(x => x.Stone)
                .WithMany(x => x.PurchaseDetails)
                .HasForeignKey(x => x.StoneId)
                .OnDelete(DeleteBehavior.Restrict);

            // Bir personelin en fazla bir kullanıcı hesabı olabilir
            modelBuilder.Entity<Employee>()
                .HasOne(x => x.User)
                .WithOne(x => x.Employee)
                .HasForeignKey<User>(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Bir alım birden fazla stok giriş hareketi oluşturabilir
            modelBuilder.Entity<StockMovement>()
                .HasOne(x => x.Purchase)
                .WithMany(x => x.StockMovements)
                .HasForeignKey(x => x.PurchaseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Bir iş emri birden fazla stok hareketi oluşturabilir
            modelBuilder.Entity<StockMovement>()
                .HasOne(x => x.WorkOrder)
                .WithMany(x => x.StockMovements)
                .HasForeignKey(x => x.WorkOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Bir kullanıcı birden fazla sistem işlem geçmişi kaydı oluşturabilir
            // Kullanıcı silinse bile işlem geçmişi korunur ve sadece UserId alanı null olur
            modelBuilder.Entity<ActivityLog>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // INVOICE - CUSTOMER İLİŞKİSİ
            // Bir müşterinin birden fazla faturası olabilir
            // Her fatura yalnızca bir müşteriye aittir
            modelBuilder.Entity<Invoice>()
                .HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);


            // INVOICE - WORK ORDER İLİŞKİSİ
            // Her fatura yalnızca bir iş emrine aittir
            // Bir iş emri için yalnızca bir fatura oluşturulabilir
            modelBuilder.Entity<Invoice>()
                .HasOne(x => x.WorkOrder)
                .WithOne()
                .HasForeignKey<Invoice>(x => x.WorkOrderId)
                .OnDelete(DeleteBehavior.Restrict);


            // INVOICE - INVOICE DETAIL İLİŞKİSİ
            // Bir faturanın birden fazla fatura kalemi olabilir
            // Her fatura kalemi yalnızca bir faturaya aittir
            modelBuilder.Entity<InvoiceDetail>()
                .HasOne(x => x.Invoice)
                .WithMany(x => x.InvoiceDetails)
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            // STONE - TAŞ AYARLARI

            // Taş adı zorunlu olacak ve en fazla 100 karakter tutulacak
            modelBuilder.Entity<Stone>()
                .Property(x => x.StoneName)
                .IsRequired()
                .HasMaxLength(100);

            // Taş rengi zorunlu değil, girilirse en fazla 50 karakter olacak
            modelBuilder.Entity<Stone>()
                .Property(x => x.Color)
                .HasMaxLength(50);

            // Taş görselinin dosya yolu girilirse en fazla 500 karakter olacak
            modelBuilder.Entity<Stone>()
                .Property(x => x.ImageUrl)
                .HasMaxLength(500);

            // Alış fiyatını virgülden sonra 2 basamak olacak şekilde saklar
            modelBuilder.Entity<Stone>()
                .Property(x => x.PurchasePrice)
                .HasPrecision(18, 2);

            // Satış fiyatını virgülden sonra 2 basamak olacak şekilde saklar
            modelBuilder.Entity<Stone>()
                .Property(x => x.SalePrice)
                .HasPrecision(18, 2);

            // Alış ve satış fiyatlarının negatif olmasını engeller
            modelBuilder.Entity<Stone>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Stone_Prices",
                    "[PurchasePrice] >= 0 AND [SalePrice] >= 0"));


            // STOCK MOVEMENT - STOK HAREKETİ AYARLARI

            // Stok hareketi açıklaması girilirse en fazla 500 karakter olabilir
            modelBuilder.Entity<StockMovement>()
                .Property(x => x.Description)
                .HasMaxLength(500);

            // Stok hareketindeki plaka adedinin 0 veya negatif olmasını engeller
            modelBuilder.Entity<StockMovement>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_StockMovement_Quantity",
                    "[Quantity] > 0"));

            // Stok hareket tipinin sadece tanımlı enum değerlerinden biri olmasını sağlar
            // Entry(1), Exit(2), AdjustmentIn(3), AdjustmentOut(4), WorkOrderReturn(5)
            modelBuilder.Entity<StockMovement>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_StockMovement_MovementType",
                    "[MovementType] IN (1, 2, 3, 4, 5)"));


            // CUSTOMER - MÜŞTERİ AYARLARI

            // Müşteri tipinin sadece bireysel veya kurumsal olmasını sağlar
            // Individual(1), Corporate(2)
            modelBuilder.Entity<Customer>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Customer_CustomerType",
                    "[CustomerType] IN (1, 2)"));

            // Bireysel müşteride ad soyad, kurumsal müşteride firma unvanı tutulur
            modelBuilder.Entity<Customer>()
                .Property(x => x.CustomerName)
                .IsRequired()
                .HasMaxLength(150);

            // T.C. kimlik numarası girilirse en fazla 11 karakter tutulur
            modelBuilder.Entity<Customer>()
                .Property(x => x.IdentityNumber)
                .HasMaxLength(11);

            // Vergi kimlik numarası girilirse en fazla 10 karakter tutulur
            modelBuilder.Entity<Customer>()
                .Property(x => x.TaxNumber)
                .HasMaxLength(10);

            // Vergi dairesi girilirse en fazla 100 karakter tutulur
            modelBuilder.Entity<Customer>()
                .Property(x => x.TaxOffice)
                .HasMaxLength(100);

            // Müşteri telefonunu zorunlu yapar ve en fazla 20 karakter tutar
            modelBuilder.Entity<Customer>()
                .Property(x => x.Phone)
                .IsRequired()
                .HasMaxLength(20);

            // E-posta adresi girilirse en fazla 150 karakter tutulur
            modelBuilder.Entity<Customer>()
                .Property(x => x.Email)
                .HasMaxLength(150);

            // Müşteri adresi girilirse en fazla 500 karakter olabilir
            modelBuilder.Entity<Customer>()
                .Property(x => x.Address)
                .HasMaxLength(500);

            // Müşteri notu girilirse en fazla 500 karakter olabilir
            modelBuilder.Entity<Customer>()
                .Property(x => x.Note)
                .HasMaxLength(500);

            // WORK ORDER - İŞ EMRİ AYARLARI

            // Kullanıcıya gösterilecek iş emri numarasını zorunlu tutar
            // Örnek: IE-2026-000001
            modelBuilder.Entity<WorkOrder>()
                .Property(x => x.WorkOrderNumber)
                .IsRequired()
                .HasMaxLength(20);

            // Aynı iş emri numarasının birden fazla kayıtta kullanılmasını engeller
            modelBuilder.Entity<WorkOrder>()
                .HasIndex(x => x.WorkOrderNumber)
                .IsUnique();

            // Atölyede yapılan kesim metresini virgülden sonra 2 basamakla saklar
            modelBuilder.Entity<WorkOrder>()
                .Property(x => x.CuttingMeter)
                .HasPrecision(18, 2);

            // Kesimin metre başına KDV hariç işçilik fiyatını saklar
            modelBuilder.Entity<WorkOrder>()
                .Property(x => x.CuttingPricePerMeter)
                .HasPrecision(18, 2);

            // Taş ve işçilik toplamının KDV hariç tutarını saklar
            modelBuilder.Entity<WorkOrder>()
                .Property(x => x.SubTotal)
                .HasPrecision(18, 2);

            // KDV oranını saklar
            // Örnek: yüzde 20 için 20.00
            modelBuilder.Entity<WorkOrder>()
                .Property(x => x.VatRate)
                .HasPrecision(5, 2);

            // Hesaplanan KDV tutarını saklar
            modelBuilder.Entity<WorkOrder>()
                .Property(x => x.VatAmount)
                .HasPrecision(18, 2);

            // KDV dahil müşteriden tahsil edilecek genel toplamı saklar
            modelBuilder.Entity<WorkOrder>()
                .Property(x => x.TotalAmount)
                .HasPrecision(18, 2);

            // İş emri açıklaması girilirse en fazla 500 karakter olabilir
            modelBuilder.Entity<WorkOrder>()
                .Property(x => x.Description)
                .HasMaxLength(500);

            // Kesim değerlerinin negatif olmasını engeller
            modelBuilder.Entity<WorkOrder>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_WorkOrder_Cutting",
                    "[CuttingMeter] >= 0 AND [CuttingPricePerMeter] >= 0"));

            // İş emrinin parasal değerlerinin negatif olmasını engeller
            modelBuilder.Entity<WorkOrder>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_WorkOrder_Amounts",
                    "[SubTotal] >= 0 AND [VatAmount] >= 0 AND [TotalAmount] >= 0"));

            // KDV oranının 0 ile 100 arasında olmasını sağlar
            modelBuilder.Entity<WorkOrder>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_WorkOrder_VatRate",
                    "[VatRate] >= 0 AND [VatRate] <= 100"));

            // İş emri durumunun sadece tanımlı enum değerlerinden biri olmasını sağlar
            // PendingApproval(1), Approved(2), InWorkshop(3),
            // Completed(4), Rejected(5), Cancelled(6)
            modelBuilder.Entity<WorkOrder>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_WorkOrder_Status",
                    "[Status] IN (1, 2, 3, 4, 5, 6)"));


            // WORK ORDER DETAIL - İŞ EMRİ DETAY AYARLARI

            // İş emri detayındaki taşın birim satış fiyatını 2 ondalık basamakla saklar
            modelBuilder.Entity<WorkOrderDetail>()
                .Property(x => x.UnitPrice)
                .HasPrecision(18, 2);

            // İş emri detayındaki açıklama veya ölçü bilgisi en fazla 500 karakter olabilir
            modelBuilder.Entity<WorkOrderDetail>()
                .Property(x => x.Description)
                .HasMaxLength(500);

            // İş emri detayındaki plaka adedinin 0 veya negatif olmasını engeller
            modelBuilder.Entity<WorkOrderDetail>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_WorkOrderDetail_Quantity",
                    "[Quantity] > 0"));

            // İş emri detayındaki birim satış fiyatının negatif olmasını engeller
            modelBuilder.Entity<WorkOrderDetail>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_WorkOrderDetail_UnitPrice",
                    "[UnitPrice] >= 0"));

            // PAYMENT - ÖDEME AYARLARI

            // Ödeme tutarını virgülden sonra 2 basamakla saklar
            modelBuilder.Entity<Payment>()
                .Property(x => x.Amount)
                .HasPrecision(18, 2);

            // Ödeme açıklaması girilirse en fazla 500 karakter olabilir
            modelBuilder.Entity<Payment>()
                .Property(x => x.Description)
                .HasMaxLength(500);

            // 0 TL veya negatif tutarlı ödeme kaydedilmesini engeller
            modelBuilder.Entity<Payment>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Payment_Amount",
                    "[Amount] > 0"));

            // Ödeme yönteminin sadece tanımlı enum değerlerinden biri olmasını sağlar
            // Cash(1), CreditCard(2), BankTransfer(3)
            modelBuilder.Entity<Payment>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Payment_PaymentMethod",
                    "[PaymentMethod] IN (1, 2, 3)"));


            // SUPPLIER - TEDARİKÇİ AYARLARI

            // Tedarikçi veya firma adını zorunlu yapar ve en fazla 150 karakter tutar
            modelBuilder.Entity<Supplier>()
                .Property(x => x.SupplierName)
                .IsRequired()
                .HasMaxLength(150);

            // Tedarikçi telefonu girilirse en fazla 20 karakter olabilir
            modelBuilder.Entity<Supplier>()
                .Property(x => x.Phone)
                .HasMaxLength(20);

            // Tedarikçi adresi girilirse en fazla 500 karakter olabilir
            modelBuilder.Entity<Supplier>()
                .Property(x => x.Address)
                .HasMaxLength(500);

            // Tedarikçi notu girilirse en fazla 500 karakter olabilir
            modelBuilder.Entity<Supplier>()
                .Property(x => x.Note)
                .HasMaxLength(500);


            // PURCHASE - ALIM AYARLARI

            // Alım numarası sistem tarafından otomatik oluşturulur
            // Örnek: ALM-2026-000001
            modelBuilder.Entity<Purchase>()
                .Property(x => x.PurchaseNumber)
                .IsRequired()
                .HasMaxLength(30);

            // Aynı alım numarasının birden fazla kayıtta kullanılmasını engeller
            modelBuilder.Entity<Purchase>()
                .HasIndex(x => x.PurchaseNumber)
                .IsUnique();

            // Alımın toplam tutarını virgülden sonra 2 basamakla saklar
            modelBuilder.Entity<Purchase>()
                .Property(x => x.TotalAmount)
                .HasPrecision(18, 2);

            // Alım açıklaması girilirse en fazla 500 karakter olabilir
            modelBuilder.Entity<Purchase>()
                .Property(x => x.Description)
                .HasMaxLength(500);

            // Alım toplam tutarının negatif olmasını engeller
            modelBuilder.Entity<Purchase>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Purchase_TotalAmount",
                    "[TotalAmount] >= 0"));


            // PURCHASE DETAIL - ALIM DETAY AYARLARI

            // Alınan taşın birim alış fiyatını 2 ondalık basamakla saklar
            modelBuilder.Entity<PurchaseDetail>()
                .Property(x => x.UnitPrice)
                .HasPrecision(18, 2);

            // Alım detayındaki plaka adedinin 0 veya negatif olmasını engeller
            modelBuilder.Entity<PurchaseDetail>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_PurchaseDetail_Quantity",
                    "[Quantity] > 0"));

            // Alım detayındaki birim alış fiyatının negatif olmasını engeller
            modelBuilder.Entity<PurchaseDetail>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_PurchaseDetail_UnitPrice",
                    "[UnitPrice] >= 0"));


            // EMPLOYEE - PERSONEL AYARLARI

            // Personel ad-soyadını zorunlu yapar ve en fazla 150 karakter tutar
            modelBuilder.Entity<Employee>()
                .Property(x => x.FullName)
                .IsRequired()
                .HasMaxLength(150);

            // Personel telefonu girilirse en fazla 20 karakter olabilir
            modelBuilder.Entity<Employee>()
                .Property(x => x.Phone)
                .HasMaxLength(20);

            // Personel e-posta adresi girilirse en fazla 150 karakter olabilir
            modelBuilder.Entity<Employee>()
                .Property(x => x.Email)
                .HasMaxLength(150);

            // Personel görevini zorunlu yapar ve en fazla 100 karakter tutar
            modelBuilder.Entity<Employee>()
                .Property(x => x.Position)
                .IsRequired()
                .HasMaxLength(100);

            // Personel notu girilirse en fazla 500 karakter olabilir
            modelBuilder.Entity<Employee>()
                .Property(x => x.Note)
                .HasMaxLength(500);

            // Personel profil fotoğrafının dosya yolu girilirse en fazla 500 karakter olabilir
            modelBuilder.Entity<Employee>()
                .Property(x => x.ImageUrl)
                .HasMaxLength(500);

            // İşten ayrılma tarihi işe giriş tarihinden önce olamaz
            modelBuilder.Entity<Employee>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Employee_TerminationDate",
                    "[TerminationDate] IS NULL OR [TerminationDate] >= [HireDate]"));


            // USER - KULLANICI AYARLARI

            // Kullanıcı adını zorunlu yapar ve en fazla 50 karakter tutar
            modelBuilder.Entity<User>()
                .Property(x => x.Username)
                .IsRequired()
                .HasMaxLength(50);

            // Şifre hash değerini zorunlu yapar ve en fazla 500 karakter tutar
            modelBuilder.Entity<User>()
                .Property(x => x.PasswordHash)
                .IsRequired()
                .HasMaxLength(500);

            // Kullanıcı rolünün sadece tanımlı enum değerlerinden biri olmasını sağlar
            // Admin(1), Manager(2), Sales(3), Accountant(4), Workshop(5)
            modelBuilder.Entity<User>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_User_Role",
                    "[Role] IN (1, 2, 3, 4, 5)"));

            // Aynı kullanıcı adıyla iki farklı hesap açılmasını engeller
            modelBuilder.Entity<User>()
                .HasIndex(x => x.Username)
                .IsUnique();


            // ACTIVITY LOG - SİSTEM İŞLEM GEÇMİŞİ AYARLARI

            // İşlemi yapan kullanıcının o andaki ad-soyad bilgisini saklar
            // Personelin adı daha sonra değişse bile eski işlem geçmişi değişmez
            modelBuilder.Entity<ActivityLog>()
                .Property(x => x.UserFullName)
                .IsRequired()
                .HasMaxLength(150);

            // İşlemi yapan hesabın o andaki kullanıcı adını saklar
            modelBuilder.Entity<ActivityLog>()
                .Property(x => x.Username)
                .IsRequired()
                .HasMaxLength(50);

            // İşlemin gerçekleştiği modül bilgisini saklar
            // Örnek: WorkOrder, Payment, Purchase, Customer
            modelBuilder.Entity<ActivityLog>()
                .Property(x => x.Module)
                .IsRequired()
                .HasMaxLength(50);

            // Gerçekleştirilen işlem türünü saklar
            // Örnek: Create, Update, Approve, Reject, Complete
            modelBuilder.Entity<ActivityLog>()
                .Property(x => x.Action)
                .IsRequired()
                .HasMaxLength(50);

            // Kullanıcıya gösterilecek işlem açıklamasını saklar
            modelBuilder.Entity<ActivityLog>()
                .Property(x => x.Description)
                .IsRequired()
                .HasMaxLength(1000);

            // IPv4 veya IPv6 adresini saklamak için en fazla 45 karakter kullanılır
            modelBuilder.Entity<ActivityLog>()
                .Property(x => x.IpAddress)
                .HasMaxLength(45);

            // Dashboard ve Son İşlemler ekranındaki tarih sıralamalarını hızlandırır
            modelBuilder.Entity<ActivityLog>()
                .HasIndex(x => x.CreatedDate);

            // Belirli bir modülün işlem geçmişini tarih sırasına göre sorgulamayı hızlandırır
            modelBuilder.Entity<ActivityLog>()
                .HasIndex(x => new
                {
                    x.Module,
                    x.CreatedDate
                });

            // Belirli bir kaydın işlem geçmişini sorgulamayı hızlandırır
            // Örnek: Belirli bir iş emrinin bütün geçmişi
            modelBuilder.Entity<ActivityLog>()
                .HasIndex(x => new
                {
                    x.Module,
                    x.RecordId,
                    x.CreatedDate
                });

            // Belirli bir kullanıcının yaptığı işlemleri tarih sırasına göre sorgulamayı hızlandırır
            modelBuilder.Entity<ActivityLog>()
                .HasIndex(x => new
                {
                    x.UserId,
                    x.CreatedDate
                });


            // INVOICE - FATURA AYARLARI

            // Fatura numarasını zorunlu yapar ve en fazla 30 karakter tutar
            // Örnek: FAT-2026-000001
            modelBuilder.Entity<Invoice>()
                .Property(x => x.InvoiceNumber)
                .IsRequired()
                .HasMaxLength(30);

            // Aynı fatura numarasının birden fazla faturada kullanılmasını engeller
            modelBuilder.Entity<Invoice>()
                .HasIndex(x => x.InvoiceNumber)
                .IsUnique();

            // Fatura oluşturulduğu andaki müşteri veya firma adını zorunlu tutar
            modelBuilder.Entity<Invoice>()
                .Property(x => x.CustomerName)
                .IsRequired()
                .HasMaxLength(150);

            // T.C. kimlik numarası girilirse en fazla 11 karakter tutulur
            modelBuilder.Entity<Invoice>()
                .Property(x => x.IdentityNumber)
                .HasMaxLength(11);

            // Vergi kimlik numarası girilirse en fazla 10 karakter tutulur
            modelBuilder.Entity<Invoice>()
                .Property(x => x.TaxNumber)
                .HasMaxLength(10);

            // Vergi dairesi girilirse en fazla 100 karakter tutulur
            modelBuilder.Entity<Invoice>()
                .Property(x => x.TaxOffice)
                .HasMaxLength(100);

            // Fatura üzerindeki müşteri adresi en fazla 500 karakter olabilir
            modelBuilder.Entity<Invoice>()
                .Property(x => x.Address)
                .HasMaxLength(500);

            // Fatura oluşturulduğu andaki satıcı firma adını saklar
            modelBuilder.Entity<Invoice>()
                .Property(x => x.SellerCompanyName)
                .HasMaxLength(200);

            // Fatura oluşturulduğu andaki satıcı firmanın vergi dairesini saklar
            modelBuilder.Entity<Invoice>()
                .Property(x => x.SellerTaxOffice)
                .HasMaxLength(100);

            // Fatura oluşturulduğu andaki satıcı firmanın vergi numarasını saklar
            modelBuilder.Entity<Invoice>()
                .Property(x => x.SellerTaxNumber)
                .HasMaxLength(20);

            // Fatura oluşturulduğu andaki satıcı firma adresini saklar
            modelBuilder.Entity<Invoice>()
                .Property(x => x.SellerAddress)
                .HasMaxLength(500);

            // Fatura oluşturulduğu andaki satıcı firma telefonunu saklar
            modelBuilder.Entity<Invoice>()
                .Property(x => x.SellerPhone)
                .HasMaxLength(30);

            // Fatura oluşturulduğu andaki satıcı firma e-posta adresini saklar
            modelBuilder.Entity<Invoice>()
                .Property(x => x.SellerEmail)
                .HasMaxLength(150);

            // Fatura oluşturulduğu andaki satıcı firma internet sitesini saklar
            modelBuilder.Entity<Invoice>()
                .Property(x => x.SellerWebsite)
                .HasMaxLength(200);

            // Fatura oluşturulduğu andaki satıcı firma logosunun yolunu saklar
            modelBuilder.Entity<Invoice>()
                .Property(x => x.SellerLogoUrl)
                .HasMaxLength(500);

            // Faturanın KDV hariç ara toplamını 2 ondalık basamakla saklar
            modelBuilder.Entity<Invoice>()
                .Property(x => x.SubTotal)
                .HasPrecision(18, 2);

            // Faturaya uygulanan KDV oranını saklar
            // Örnek: yüzde 20 için 20.00
            modelBuilder.Entity<Invoice>()
                .Property(x => x.VatRate)
                .HasPrecision(5, 2);

            // Faturanın toplam KDV tutarını 2 ondalık basamakla saklar
            modelBuilder.Entity<Invoice>()
                .Property(x => x.VatAmount)
                .HasPrecision(18, 2);

            // Faturanın KDV dahil genel toplamını 2 ondalık basamakla saklar
            modelBuilder.Entity<Invoice>()
                .Property(x => x.TotalAmount)
                .HasPrecision(18, 2);

            // Fatura açıklaması girilirse en fazla 500 karakter olabilir
            modelBuilder.Entity<Invoice>()
                .Property(x => x.Description)
                .HasMaxLength(500);

            // Fatura iptal nedenini en fazla 500 karakter olarak saklar
            modelBuilder.Entity<Invoice>()
                .Property(x => x.CancellationReason)
                .HasMaxLength(500);

            // Faturadaki parasal değerlerin negatif olmasını engeller
            modelBuilder.Entity<Invoice>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Invoice_Amounts",
                    "[SubTotal] >= 0 AND [VatAmount] >= 0 AND [TotalAmount] >= 0"));

            // Faturadaki KDV oranının 0 ile 100 arasında olmasını sağlar
            modelBuilder.Entity<Invoice>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Invoice_VatRate",
                    "[VatRate] >= 0 AND [VatRate] <= 100"));

            // Fatura durumunun sadece tanımlı enum değerlerinden biri olmasını sağlar
            // Draft(1), Issued(2), Cancelled(3)
            modelBuilder.Entity<Invoice>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Invoice_Status",
                    "[Status] IN (1, 2, 3)"));

            // INVOICE DETAIL - FATURA DETAY AYARLARI

            // Fatura kaleminin açıklamasını zorunlu yapar ve en fazla 500 karakter tutar
            modelBuilder.Entity<InvoiceDetail>()
                .Property(x => x.Description)
                .IsRequired()
                .HasMaxLength(500);

            // Fatura kaleminin miktarını 2 ondalık basamakla saklar
            // Plaka için 2, kesim işçiliği için 15.50 metre gibi değerler tutulabilir
            modelBuilder.Entity<InvoiceDetail>()
                .Property(x => x.Quantity)
                .HasPrecision(18, 2);

            // Fatura kaleminin birimini zorunlu yapar
            // Örnek: Plaka, Metre
            modelBuilder.Entity<InvoiceDetail>()
                .Property(x => x.Unit)
                .IsRequired()
                .HasMaxLength(30);

            // Birim fiyatını 2 ondalık basamakla saklar
            modelBuilder.Entity<InvoiceDetail>()
                .Property(x => x.UnitPrice)
                .HasPrecision(18, 2);

            // Fatura kaleminin KDV hariç toplamını saklar
            modelBuilder.Entity<InvoiceDetail>()
                .Property(x => x.SubTotal)
                .HasPrecision(18, 2);

            // Fatura kalemine uygulanan KDV oranını saklar
            modelBuilder.Entity<InvoiceDetail>()
                .Property(x => x.VatRate)
                .HasPrecision(5, 2);

            // Fatura kaleminin KDV tutarını saklar
            modelBuilder.Entity<InvoiceDetail>()
                .Property(x => x.VatAmount)
                .HasPrecision(18, 2);

            // Fatura kaleminin KDV dahil toplamını saklar
            modelBuilder.Entity<InvoiceDetail>()
                .Property(x => x.TotalAmount)
                .HasPrecision(18, 2);

            // Fatura kaleminde 0 veya negatif miktar girilmesini engeller
            modelBuilder.Entity<InvoiceDetail>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_InvoiceDetail_Quantity",
                    "[Quantity] > 0"));

            // Fatura kalemindeki parasal değerlerin negatif olmasını engeller
            modelBuilder.Entity<InvoiceDetail>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_InvoiceDetail_Amounts",
                    "[UnitPrice] >= 0 AND [SubTotal] >= 0 AND [VatAmount] >= 0 AND [TotalAmount] >= 0"));

            // Fatura kalemindeki KDV oranının 0 ile 100 arasında olmasını sağlar
            modelBuilder.Entity<InvoiceDetail>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_InvoiceDetail_VatRate",
                    "[VatRate] >= 0 AND [VatRate] <= 100"));
            // COMPANY SETTING AYARLARI
            // Sistemi kullanan işletmenin fatura ve kurumsal belgelerde
            // kullanılacak firma bilgilerinin veritabanı kurallarını belirler
            modelBuilder.Entity<CompanySetting>(entity =>
            {
                // Firma unvanı zorunludur ve en fazla 200 karakter olabilir
                entity.Property(x => x.CompanyName)
                    .IsRequired()
                    .HasMaxLength(200);

                // Vergi dairesi zorunludur ve en fazla 100 karakter olabilir
                entity.Property(x => x.TaxOffice)
                    .IsRequired()
                    .HasMaxLength(100);

                // Vergi numarası zorunludur ve en fazla 10 karakter olabilir
                entity.Property(x => x.TaxNumber)
                    .IsRequired()
                    .HasMaxLength(10);

                // Firma adresi zorunludur ve en fazla 500 karakter olabilir
                entity.Property(x => x.Address)
                    .IsRequired()
                    .HasMaxLength(500);

                // Telefon numarası en fazla 20 karakter olabilir
                entity.Property(x => x.Phone)
                    .HasMaxLength(20);

                // E-posta adresi en fazla 150 karakter olabilir
                entity.Property(x => x.Email)
                    .HasMaxLength(150);

                // İnternet sitesi adresi en fazla 200 karakter olabilir
                entity.Property(x => x.Website)
                    .HasMaxLength(200);

                // Firma logosunun dosya yolu en fazla 500 karakter olabilir
                entity.Property(x => x.LogoUrl)
                    .HasMaxLength(500);

                // Instagram bağlantısı en fazla 300 karakter olabilir
                entity.Property(x => x.InstagramUrl)
                    .HasMaxLength(300);

                // Facebook bağlantısı en fazla 300 karakter olabilir
                entity.Property(x => x.FacebookUrl)
                    .HasMaxLength(300);

                // WhatsApp bağlantısı en fazla 300 karakter olabilir
                entity.Property(x => x.WhatsAppUrl)
                    .HasMaxLength(300);



                // CONTACT MESSAGE - İLETİŞİM MESAJI AYARLARI

                // Mesajı gönderen kişinin adı ve soyadı zorunludur
                modelBuilder.Entity<ContactMessage>()
                    .Property(x => x.FullName)
                    .IsRequired()
                    .HasMaxLength(150);

                // Gönderen kişinin e-posta adresi zorunludur
                modelBuilder.Entity<ContactMessage>()
                    .Property(x => x.Email)
                    .IsRequired()
                    .HasMaxLength(150);

                // Mesajın konusu zorunludur
                modelBuilder.Entity<ContactMessage>()
                    .Property(x => x.Subject)
                    .IsRequired()
                    .HasMaxLength(200);

                // Mesaj içeriği zorunludur
                modelBuilder.Entity<ContactMessage>()
                    .Property(x => x.Message)
                    .IsRequired()
                    .HasMaxLength(5000);

                // Yönetici yanıtı en fazla 5000 karakter olabilir
                modelBuilder.Entity<ContactMessage>()
                    .Property(x => x.ReplyMessage)
                    .HasMaxLength(5000);

                // Mesajları gönderilme tarihine göre sıralamayı hızlandırır
                modelBuilder.Entity<ContactMessage>()
                    .HasIndex(x => x.CreatedAt);

            });


        }
    }
}