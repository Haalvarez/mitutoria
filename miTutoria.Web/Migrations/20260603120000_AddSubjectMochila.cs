using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace miTutoria.Web.Migrations;

/// <summary>
/// "Mochila": un alumno puede tener varias materias (cuadernos), cada una con su
/// historia de chat y material. Agrega name, mode y last_active_at a classrooms.
/// Aplicar manualmente en TablePlus (ver bloque SQL del cierre de sesión).
/// </summary>
public partial class AddSubjectMochila : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            ALTER TABLE academic.classrooms ADD COLUMN IF NOT EXISTS ""name"" text NOT NULL DEFAULT 'General';
            ALTER TABLE academic.classrooms ADD COLUMN IF NOT EXISTS ""mode"" integer NOT NULL DEFAULT 1;
            ALTER TABLE academic.classrooms ADD COLUMN IF NOT EXISTS ""last_active_at"" timestamptz NOT NULL DEFAULT now();
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            ALTER TABLE academic.classrooms DROP COLUMN IF EXISTS ""name"";
            ALTER TABLE academic.classrooms DROP COLUMN IF EXISTS ""mode"";
            ALTER TABLE academic.classrooms DROP COLUMN IF EXISTS ""last_active_at"";
        ");
    }
}
