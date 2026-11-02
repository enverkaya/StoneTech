using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoneTech.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Alım numarası kolonunu önce nullable olarak ekler.
            // Böylece mevcut kayıtlar migration sırasında sorun çıkarmaz.
            migrationBuilder.AddColumn<string>(
                name: "PurchaseNumber",
                table: "Purchases",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            // Veritabanındaki mevcut alımlara otomatik alım numarası verir.
            // Örnek: PurchaseId 1 -> ALM-2026-000001
            migrationBuilder.Sql(@"
                UPDATE Purchases
                SET PurchaseNumber =
                    'ALM-' +
                    CAST(YEAR(PurchaseDate) AS varchar(4)) +
                    '-' +
                    RIGHT('000000' + CAST(PurchaseId AS varchar(6)), 6)
                WHERE PurchaseNumber IS NULL;
            ");

            // Mevcut kayıtlar doldurulduktan sonra alanı zorunlu hale getirir.
            migrationBuilder.AlterColumn<string>(
                name: "PurchaseNumber",
                table: "Purchases",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldNullable: true);

            // İki alımın aynı alım numarasına sahip olmasını engeller.
            migrationBuilder.CreateIndex(
                name: "IX_Purchases_PurchaseNumber",
                table: "Purchases",
                column: "PurchaseNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Purchases_PurchaseNumber",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "PurchaseNumber",
                table: "Purchases");
        }
    }
}