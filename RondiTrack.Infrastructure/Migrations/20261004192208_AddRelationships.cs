using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RondiTrack.Migrations
{
    /// <inheritdoc />
    public partial class AddRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "JoinedAtUtc",
                table: "StokvelMembers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "StokvelMembers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JoinedAtUtc",
                table: "StokvelMembers");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "StokvelMembers");
        }
    }
}
