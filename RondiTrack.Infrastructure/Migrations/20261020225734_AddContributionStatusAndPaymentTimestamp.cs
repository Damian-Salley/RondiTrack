using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RondiTrack.Migrations
{
    // Adds payment statuses and UTC payment timestamps
    // without removing existing contribution records.
    public partial class AddContributionStatusAndPaymentTimestamp : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add a nullable payment timestamp.
            // Existing contributions retain NULL because their
            // original payment timestamps were never recorded.
            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAtUtc",
                table: "Contributions",
                type: "timestamp with time zone",
                nullable: true);

            // Existing contributions are treated as Paid because
            // the previous workflow recorded completed payments.
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Contributions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Paid");

            // Only permit the three supported contribution statuses.
            migrationBuilder.AddCheckConstraint(
                name: "CK_Contributions_Status",
                table: "Contributions",
                sql: "\"Status\" IN ('Pending', 'Paid', 'Confirmed')");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the constraint before removing its column.
            migrationBuilder.DropCheckConstraint(
                name: "CK_Contributions_Status",
                table: "Contributions");

            // Remove the fields introduced by this migration.
            migrationBuilder.DropColumn(
                name: "PaidAtUtc",
                table: "Contributions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Contributions");
        }
    }
}