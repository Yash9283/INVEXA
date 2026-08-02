using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockFlow.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityQuestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SecurityAnswer",
                table: "Admins",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityQuestion",
                table: "Admins",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecurityAnswer",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "SecurityQuestion",
                table: "Admins");
        }
    }
}
