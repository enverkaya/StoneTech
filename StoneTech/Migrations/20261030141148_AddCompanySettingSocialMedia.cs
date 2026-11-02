using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoneTech.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanySettingSocialMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FacebookUrl",
                table: "CompanySettings",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstagramUrl",
                table: "CompanySettings",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppUrl",
                table: "CompanySettings",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FacebookUrl",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "InstagramUrl",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "WhatsAppUrl",
                table: "CompanySettings");
        }
    }
}
