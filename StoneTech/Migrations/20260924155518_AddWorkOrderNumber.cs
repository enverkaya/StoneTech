using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoneTech.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkOrderNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // İş emri numarası kolonunu önce boş değer kabul edecek şekilde ekler
            migrationBuilder.AddColumn<string>(
                name: "WorkOrderNumber",
                table: "WorkOrders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            // Sistemde daha önce oluşturulmuş iş emirlerine numara verir
            // Numara oluşturulurken iş emrinin oluşturulduğu yıl kullanılır
            // Örnek: IE-2026-000001
            migrationBuilder.Sql(@"
                UPDATE WorkOrders
                SET WorkOrderNumber =
                    'IE-' +
                    CAST(YEAR(CreatedDate) AS varchar(4)) +
                    '-' +
                    RIGHT(
                        '000000' + CAST(WorkOrderId AS varchar(6)),
                        6
                    )
                WHERE WorkOrderNumber IS NULL;
            ");

            // Eski kayıtlar numaralandırıldıktan sonra alanı zorunlu hale getirir
            migrationBuilder.AlterColumn<string>(
                name: "WorkOrderNumber",
                table: "WorkOrders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            // Aynı iş emri numarasının iki farklı kayıtta bulunmasını engeller
            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_WorkOrderNumber",
                table: "WorkOrders",
                column: "WorkOrderNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // İş emri numarası üzerindeki benzersiz indexi kaldırır
            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_WorkOrderNumber",
                table: "WorkOrders");

            // İş emri numarası kolonunu kaldırır
            migrationBuilder.DropColumn(
                name: "WorkOrderNumber",
                table: "WorkOrders");
        }
    }
}