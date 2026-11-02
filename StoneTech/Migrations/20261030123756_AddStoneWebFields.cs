using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoneTech.Migrations
{
    /// <inheritdoc />
    public partial class AddStoneWebFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowOnWeb",
                table: "Stones",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "WebDescription",
                table: "Stones",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowOnWeb",
                table: "Stones");

            migrationBuilder.DropColumn(
                name: "WebDescription",
                table: "Stones");
        }
    }
}
