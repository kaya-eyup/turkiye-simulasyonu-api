using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TurkiyeSimulasyonu.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRebirthVotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RebirthVotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VoterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CountryCode = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RebirthVotes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RebirthVotes_CountryCode",
                table: "RebirthVotes",
                column: "CountryCode");

            migrationBuilder.CreateIndex(
                name: "IX_RebirthVotes_VoterId",
                table: "RebirthVotes",
                column: "VoterId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RebirthVotes");
        }
    }
}
