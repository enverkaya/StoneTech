using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoneTech.Migrations
{
    /// <inheritdoc />
    public partial class ProfessionalizeCommercialStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Eski kısıtları veri dönüşümünden önce kaldırır
            migrationBuilder.DropCheckConstraint(
                name: "CK_WorkOrder_Status",
                table: "WorkOrders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_WorkOrder_TotalAmount",
                table: "WorkOrders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_User_Role",
                table: "Users");

            // Yeni ticari alanları ekler
            migrationBuilder.AddColumn<decimal>(
                name: "SubTotal",
                table: "WorkOrders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatAmount",
                table: "WorkOrders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                table: "WorkOrders",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Mevcut müşteriler bireysel müşteri olarak taşınır
            migrationBuilder.AddColumn<int>(
                name: "CustomerType",
                table: "Customers",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Customers",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdentityNumber",
                table: "Customers",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxNumber",
                table: "Customers",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxOffice",
                table: "Customers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // Eski iş emirlerinin parasal geçmişini bozmadan yeni alanları doldurur
            // Eski TotalAmount aynen korunur ve geçmiş kayıtlara geriye dönük KDV eklenmez
            migrationBuilder.Sql(
                """
                UPDATE [WorkOrders]
                SET
                    [SubTotal] = [TotalAmount],
                    [VatRate] = 0,
                    [VatAmount] = 0;
                """);

            // Eski iş emri durumlarını yeni iş akışındaki karşılıklarına dönüştürür
            // 1 Bekliyor   -> 1 Onay Bekliyor
            // 2 İşlemde    -> 3 Atölyede
            // 3 Tamamlandı -> 4 Tamamlandı
            // 4 İptal      -> 6 İptal
            migrationBuilder.Sql(
                """
                UPDATE [WorkOrders]
                SET [Status] =
                    CASE [Status]
                        WHEN 1 THEN 1
                        WHEN 2 THEN 3
                        WHEN 3 THEN 4
                        WHEN 4 THEN 6
                        ELSE [Status]
                    END;
                """);

            // Eski genel Employee rolünü Workshop rolüne dönüştürür
            // Admin ve Manager değerleri değişmeden korunur
            migrationBuilder.Sql(
                """
                UPDATE [Users]
                SET [Role] = 5
                WHERE [Role] = 3;
                """);

            // Yeni kısıtları ekler
            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkOrder_Amounts",
                table: "WorkOrders",
                sql: "[SubTotal] >= 0 AND [VatAmount] >= 0 AND [TotalAmount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkOrder_Status",
                table: "WorkOrders",
                sql: "[Status] IN (1, 2, 3, 4, 5, 6)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkOrder_VatRate",
                table: "WorkOrders",
                sql: "[VatRate] >= 0 AND [VatRate] <= 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_User_Role",
                table: "Users",
                sql: "[Role] IN (1, 2, 3, 4, 5)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Customer_CustomerType",
                table: "Customers",
                sql: "[CustomerType] IN (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Yeni kısıtları veri dönüşümünden önce kaldırır
            migrationBuilder.DropCheckConstraint(
                name: "CK_WorkOrder_Amounts",
                table: "WorkOrders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_WorkOrder_Status",
                table: "WorkOrders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_WorkOrder_VatRate",
                table: "WorkOrders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_User_Role",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Customer_CustomerType",
                table: "Customers");

            // Yeni iş emri durumlarını eski dört durumlu yapıya geri dönüştürür
            // Approved eski yapıda ayrı olmadığı için Bekliyor'a,
            // Rejected eski yapıda ayrı olmadığı için İptal'e dönüştürülür
            migrationBuilder.Sql(
                """
                UPDATE [WorkOrders]
                SET [Status] =
                    CASE [Status]
                        WHEN 1 THEN 1
                        WHEN 2 THEN 1
                        WHEN 3 THEN 2
                        WHEN 4 THEN 3
                        WHEN 5 THEN 4
                        WHEN 6 THEN 4
                        ELSE [Status]
                    END;
                """);

            // Yeni çalışan rollerini eski Employee rolüne geri dönüştürür
            migrationBuilder.Sql(
                """
                UPDATE [Users]
                SET [Role] = 3
                WHERE [Role] IN (3, 4, 5);
                """);

            // Yeni kolonları kaldırır
            migrationBuilder.DropColumn(
                name: "SubTotal",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "VatAmount",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "VatRate",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "CustomerType",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IdentityNumber",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "TaxNumber",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "TaxOffice",
                table: "Customers");

            // Eski kısıtları geri ekler
            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkOrder_Status",
                table: "WorkOrders",
                sql: "[Status] IN (1, 2, 3, 4)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkOrder_TotalAmount",
                table: "WorkOrders",
                sql: "[TotalAmount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_User_Role",
                table: "Users",
                sql: "[Role] IN (1, 2, 3)");
        }
    }
}