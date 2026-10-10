using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RondiTrack.Migrations
{
    /// <inheritdoc />
    public partial class AddContributionMemberRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_Contributions_StokvelMembers_StokvelId_UserId",
                table: "Contributions",
                columns: new[] { "StokvelId", "UserId" },
                principalTable: "StokvelMembers",
                principalColumns: new[] { "StokvelId", "UserId" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contributions_StokvelMembers_StokvelId_UserId",
                table: "Contributions");
        }
    }
}
