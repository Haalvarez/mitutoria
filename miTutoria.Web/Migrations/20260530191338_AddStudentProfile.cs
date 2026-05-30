using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace miTutoria.Web.Migrations;

public partial class AddStudentProfile : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "nickname",
            schema: "auth",
            table: "users",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "school_level",
            schema: "auth",
            table: "users",
            nullable: false,
            defaultValue: "Primario");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "nickname", schema: "auth", table: "users");
        migrationBuilder.DropColumn(name: "school_level", schema: "auth", table: "users");
    }
}
