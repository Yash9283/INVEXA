using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockFlow.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilePhotoToAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfilePhoto",
                table: "Admins",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePhoto",
                table: "Admins");
        }
    }
}
