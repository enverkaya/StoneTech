using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoneTech.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceSellerSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SellerAddress",
                table: "Invoices",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerCompanyName",
                table: "Invoices",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerEmail",
                table: "Invoices",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerLogoUrl",
                table: "Invoices",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerPhone",
                table: "Invoices",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerTaxNumber",
                table: "Invoices",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerTaxOffice",
                table: "Invoices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerWebsite",
                table: "Invoices",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SellerAddress",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SellerCompanyName",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SellerEmail",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SellerLogoUrl",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SellerPhone",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SellerTaxNumber",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SellerTaxOffice",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SellerWebsite",
                table: "Invoices");
        }
    }
}
