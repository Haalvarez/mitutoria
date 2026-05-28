using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace miTutoria.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMagicTokenToFamily : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                schema: "auth",
                table: "families",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MagicToken",
                schema: "auth",
                table: "families",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MagicTokenExpiry",
                schema: "auth",
                table: "families",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                schema: "auth",
                table: "families");

            migrationBuilder.DropColumn(
                name: "MagicToken",
                schema: "auth",
                table: "families");

            migrationBuilder.DropColumn(
                name: "MagicTokenExpiry",
                schema: "auth",
                table: "families");
        }
    }
}
