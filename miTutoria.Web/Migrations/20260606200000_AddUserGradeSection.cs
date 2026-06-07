using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using miTutoria.Web.Data;

#nullable disable

namespace miTutoria.Web.Migrations
{
    /// <summary>
    /// Grado + división del alumno (ej. "1ro A"), extraído del nombre crudo del curso de Classroom.
    /// Idempotente (IF NOT EXISTS).
    /// Lleva [DbContext] + [Migration] para que db.Database.Migrate() la aplique sola en el arranque.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260606200000_AddUserGradeSection")]
    public partial class AddUserGradeSection : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE auth.users ADD COLUMN IF NOT EXISTS grade_section text NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE auth.users DROP COLUMN IF EXISTS grade_section;");
        }
    }
}
