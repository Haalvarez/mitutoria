using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace miTutoria.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddParentProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Nickname",
                schema: "auth",
                table: "families",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentRole",
                schema: "auth",
                table: "families",
                type: "text",
                nullable: false,
                defaultValue: "Padre");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Nickname",
                schema: "auth",
                table: "families");

            migrationBuilder.DropColumn(
                name: "ParentRole",
                schema: "auth",
                table: "families");
        }
    }
}
