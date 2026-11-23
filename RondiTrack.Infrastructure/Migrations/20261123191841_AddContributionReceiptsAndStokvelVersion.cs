using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RondiTrack.Migrations
{
    /// <inheritdoc />
    public partial class AddContributionReceiptsAndStokvelVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "Version",
                table: "Stokvels",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "ContributionReceipts",
                columns: table => new
                {
                    Key = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    StokvelId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CycleId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContributionReceipts", x => x.Key);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContributionReceipts_ExpiresAtUtc",
                table: "ContributionReceipts",
                column: "ExpiresAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContributionReceipts");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Stokvels");
        }
    }
}
