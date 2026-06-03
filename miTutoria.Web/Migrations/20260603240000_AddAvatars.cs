using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace miTutoria.Web.Migrations;

/// <summary>
/// Personalización: cara del alumno + nombre y cara del tutor (galería fija de emojis).
/// Idempotente — corre sola en el arranque.
/// </summary>
public partial class AddAvatars : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            ALTER TABLE auth.users ADD COLUMN IF NOT EXISTS avatar text;
            ALTER TABLE auth.users ADD COLUMN IF NOT EXISTS tutor_name text;
            ALTER TABLE auth.users ADD COLUMN IF NOT EXISTS tutor_avatar text;
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            ALTER TABLE auth.users DROP COLUMN IF EXISTS avatar;
            ALTER TABLE auth.users DROP COLUMN IF EXISTS tutor_name;
            ALTER TABLE auth.users DROP COLUMN IF EXISTS tutor_avatar;
        ");
    }
}
