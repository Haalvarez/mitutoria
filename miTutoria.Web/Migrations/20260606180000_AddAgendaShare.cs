using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using miTutoria.Web.Data;

#nullable disable

namespace miTutoria.Web.Migrations
{
    /// <summary>
    /// Calendario público compartible: token por alumno (opt-in) + tabla de visitas (analítica first-party).
    /// Idempotente (IF NOT EXISTS).
    /// Lleva [DbContext] + [Migration] para que db.Database.Migrate() la aplique sola en el arranque.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260606180000_AddAgendaShare")]
    public partial class AddAgendaShare : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE auth.users ADD COLUMN IF NOT EXISTS agenda_share_token text NULL;
                CREATE UNIQUE INDEX IF NOT EXISTS ix_users_agenda_share_token
                    ON auth.users (agenda_share_token) WHERE agenda_share_token IS NOT NULL;

                CREATE SCHEMA IF NOT EXISTS academic;
                CREATE TABLE IF NOT EXISTS academic.agenda_views (
                    id          serial PRIMARY KEY,
                    student_id  integer NOT NULL,
                    viewed_at   timestamptz NOT NULL DEFAULT now(),
                    referrer    text NULL
                );
                CREATE INDEX IF NOT EXISTS ix_agenda_views_student_id
                    ON academic.agenda_views (student_id);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS academic.agenda_views;
                ALTER TABLE auth.users DROP COLUMN IF EXISTS agenda_share_token;
            ");
        }
    }
}
