using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace miTutoria.Web.Migrations;

/// <summary>
/// Intereses personales del alumno (fútbol, manga, etc.) para que el tutor conecte.
/// Idempotente — corre sola en el arranque vía db.Database.Migrate().
/// </summary>
public partial class AddInterestsToUser : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"ALTER TABLE auth.users ADD COLUMN IF NOT EXISTS interests text;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"ALTER TABLE auth.users DROP COLUMN IF EXISTS interests;");
    }
}
