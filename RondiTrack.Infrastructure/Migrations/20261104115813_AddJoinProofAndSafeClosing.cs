using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RondiTrack.Migrations
{
    /// <inheritdoc />
    public partial class AddJoinProofAndSafeClosing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CycleCloseReceipts",
                columns: table => new
                {
                    CycleId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    ResultJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CycleCloseReceipts", x => x.CycleId);
                    table.ForeignKey(
                        name: "FK_CycleCloseReceipts_ContributionCycles_CycleId",
                        column: x => x.CycleId,
                        principalTable: "ContributionCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JoinInvites",
                columns: table => new
                {
                    Code = table.Column<string>(type: "text", nullable: false),
                    StokvelId = table.Column<int>(type: "integer", nullable: false),
                    Rules = table.Column<string>(type: "text", nullable: false),
                    Schedule = table.Column<string>(type: "text", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JoinInvites", x => x.Code);
                    table.ForeignKey(
                        name: "FK_JoinInvites_Stokvels_StokvelId",
                        column: x => x.StokvelId,
                        principalTable: "Stokvels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentProofs",
                columns: table => new
                {
                    StokvelId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Cycle = table.Column<int>(type: "integer", nullable: false),
                    UploaderId = table.Column<int>(type: "integer", nullable: false),
                    Bytes = table.Column<byte[]>(type: "bytea", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentProofs", x => new { x.StokvelId, x.UserId, x.Cycle });
                    table.ForeignKey(
                        name: "FK_PaymentProofs_Contributions_StokvelId_UserId_Cycle",
                        columns: x => new { x.StokvelId, x.UserId, x.Cycle },
                        principalTable: "Contributions",
                        principalColumns: new[] { "StokvelId", "UserId", "Cycle" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JoinInvites_StokvelId",
                table: "JoinInvites",
                column: "StokvelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CycleCloseReceipts");

            migrationBuilder.DropTable(
                name: "JoinInvites");

            migrationBuilder.DropTable(
                name: "PaymentProofs");
        }
    }
}
