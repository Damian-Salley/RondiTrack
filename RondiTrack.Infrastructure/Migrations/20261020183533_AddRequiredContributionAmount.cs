using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RondiTrack.Migrations
{
    // Adds the fixed per-member contribution amount to contribution cycles.
    public partial class AddRequiredContributionAmount : Migration
    {
        // Applies the database changes.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add the new column.
            // Existing rows initially receive 0 until the backfill runs.
            migrationBuilder.AddColumn<decimal>(
                name: "RequiredContributionAmount",
                table: "ContributionCycles",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            // Copy each stokvel's current per-member contribution amount
            // into its existing contribution cycles.
            //
            // PostgreSQL requires quoted identifiers because EF Core
            // created these tables and columns using PascalCase names.
            migrationBuilder.Sql("""
                UPDATE "ContributionCycles" AS cycle
                SET "RequiredContributionAmount" = stokvel."ContributionAmount"
                FROM "Stokvels" AS stokvel
                WHERE cycle."StokvelId" = stokvel."Id";
                """);
        }

        // Reverts the database changes if the migration is rolled back.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the per-member contribution amount column.
            migrationBuilder.DropColumn(
                name: "RequiredContributionAmount",
                table: "ContributionCycles");
        }
    }
}