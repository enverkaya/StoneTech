using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoneTech.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkOrderReturnStockMovement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovement_MovementType",
                table: "StockMovements");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovement_MovementType",
                table: "StockMovements",
                sql: "[MovementType] IN (1, 2, 3, 4, 5)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovement_MovementType",
                table: "StockMovements");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovement_MovementType",
                table: "StockMovements",
                sql: "[MovementType] IN (1, 2, 3, 4)");
        }
    }
}
