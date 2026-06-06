using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using miTutoria.Web.Data;

#nullable disable

namespace miTutoria.Web.Migrations
{
    /// <summary>
    /// Atención dedicada — tabla de sesiones de foco del Aula (Page Visibility + foco de ventana).
    /// Idempotente (IF NOT EXISTS). Schema 'academic'. Upsert por (student_id, client_key).
    /// Lleva [DbContext] + [Migration] para que db.Database.Migrate() la aplique sola en el arranque.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260606130000_AddFocusSessions")]
    public partial class AddFocusSessions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE SCHEMA IF NOT EXISTS academic;

                CREATE TABLE IF NOT EXISTS academic.focus_sessions (
                    id             serial PRIMARY KEY,
                    student_id     integer NOT NULL,
                    client_key     text NOT NULL,
                    started_at     timestamptz NOT NULL DEFAULT now(),
                    last_beat_at   timestamptz NOT NULL DEFAULT now(),
                    focused_ms     bigint NOT NULL DEFAULT 0,
                    idle_ms        bigint NOT NULL DEFAULT 0,
                    away_ms        bigint NOT NULL DEFAULT 0,
                    interruptions  integer NOT NULL DEFAULT 0
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_focus_sessions_student_key
                    ON academic.focus_sessions (student_id, client_key);
                CREATE INDEX IF NOT EXISTS ix_focus_sessions_student_id
                    ON academic.focus_sessions (student_id);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS academic.focus_sessions;");
        }
    }
}
