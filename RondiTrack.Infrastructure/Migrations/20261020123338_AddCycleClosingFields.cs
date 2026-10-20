using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RondiTrack.Migrations
{
    public partial class AddCycleClosingFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAtUtc",
                table: "ContributionCycles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndsAtUtc",
                table: "ContributionCycles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            // Existing cycles are assigned the beginning of next month
            // in South African time.
            migrationBuilder.Sql("""
                UPDATE "ContributionCycles"
                SET "EndsAtUtc" =
                    (
                        date_trunc(
                            'month',
                            CURRENT_TIMESTAMP AT TIME ZONE 'Africa/Johannesburg'
                        ) + INTERVAL '1 month'
                    ) AT TIME ZONE 'Africa/Johannesburg';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClosedAtUtc",
                table: "ContributionCycles");

            migrationBuilder.DropColumn(
                name: "EndsAtUtc",
                table: "ContributionCycles");
        }
    }
}