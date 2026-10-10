using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RondiTrack.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Stokvels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ContributionAmount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stokvels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContributionCycles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StokvelId = table.Column<int>(type: "integer", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    TargetAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContributionCycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContributionCycles_Stokvels_StokvelId",
                        column: x => x.StokvelId,
                        principalTable: "Stokvels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StokvelMembers",
                columns: table => new
                {
                    StokvelId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StokvelMembers", x => new { x.StokvelId, x.UserId });
                    table.ForeignKey(
                        name: "FK_StokvelMembers_Stokvels_StokvelId",
                        column: x => x.StokvelId,
                        principalTable: "Stokvels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StokvelMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Contributions",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    StokvelId = table.Column<int>(type: "integer", nullable: false),
                    Cycle = table.Column<int>(type: "integer", nullable: false),
                    ContributionAmount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contributions", x => new { x.StokvelId, x.UserId, x.Cycle });
                    table.ForeignKey(
                        name: "FK_Contributions_ContributionCycles_Cycle",
                        column: x => x.Cycle,
                        principalTable: "ContributionCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contributions_Stokvels_StokvelId",
                        column: x => x.StokvelId,
                        principalTable: "Stokvels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contributions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StokvelId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ContributionCycleId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    PayoutDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RotationOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payouts_ContributionCycles_ContributionCycleId",
                        column: x => x.ContributionCycleId,
                        principalTable: "ContributionCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Payouts_Stokvels_StokvelId",
                        column: x => x.StokvelId,
                        principalTable: "Stokvels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Payouts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContributionCycles_StokvelId",
                table: "ContributionCycles",
                column: "StokvelId");

            migrationBuilder.CreateIndex(
                name: "IX_Contributions_Cycle",
                table: "Contributions",
                column: "Cycle");

            migrationBuilder.CreateIndex(
                name: "IX_Contributions_UserId",
                table: "Contributions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_ContributionCycleId",
                table: "Payouts",
                column: "ContributionCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_StokvelId",
                table: "Payouts",
                column: "StokvelId");

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_UserId",
                table: "Payouts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StokvelMembers_UserId",
                table: "StokvelMembers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Contributions");

            migrationBuilder.DropTable(
                name: "Payouts");

            migrationBuilder.DropTable(
                name: "StokvelMembers");

            migrationBuilder.DropTable(
                name: "ContributionCycles");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Stokvels");
        }
    }
}
