using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TurkiyeSimulasyonu.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddItemSearchText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SearchText",
                table: "Items",
                type: "nvarchar(700)",
                maxLength: 700,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SearchText",
                table: "Items");
        }
    }
}
