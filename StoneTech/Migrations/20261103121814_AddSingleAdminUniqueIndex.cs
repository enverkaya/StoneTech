using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoneTech.Migrations
{
    /// <inheritdoc />
    public partial class AddSingleAdminUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Admin index'i mevcut değilse oluşturur
            migrationBuilder.Sql(@"
        IF NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE name = N'UX_Users_SingleAdmin'
              AND object_id = OBJECT_ID(N'dbo.Users')
        )
        BEGIN
            CREATE UNIQUE INDEX UX_Users_SingleAdmin
            ON dbo.Users (Role)
            WHERE Role = 1;
        END
    ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Users_SingleAdmin",
                table: "Users");
        }
    }
}
