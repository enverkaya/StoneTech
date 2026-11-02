using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoneTech.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkOrderCuttingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CuttingMeter",
                table: "WorkOrders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CuttingPricePerMeter",
                table: "WorkOrders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkOrder_Cutting",
                table: "WorkOrders",
                sql: "[CuttingMeter] >= 0 AND [CuttingPricePerMeter] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_WorkOrder_Cutting",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "CuttingMeter",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "CuttingPricePerMeter",
                table: "WorkOrders");
        }
    }
}
