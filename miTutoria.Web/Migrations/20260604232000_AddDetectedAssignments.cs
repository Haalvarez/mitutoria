using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using miTutoria.Web.Data;

#nullable disable

namespace miTutoria.Web.Migrations
{
    /// <summary>
    /// Track 2 — Capa 2b: tareas/materiales/anuncios estructurados detectados por el parser.
    /// Idempotente. Dedup por (student_id, item_id) cuando hay item_id (índice único parcial).
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260604232000_AddDetectedAssignments")]
    public partial class AddDetectedAssignments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE SCHEMA IF NOT EXISTS inbox;

                CREATE TABLE IF NOT EXISTS inbox.detected_assignments (
                    id            serial PRIMARY KEY,
                    student_id    integer NOT NULL,
                    classroom_id  integer NULL,
                    type          text NOT NULL DEFAULT '',
                    title         text NOT NULL DEFAULT '',
                    course_name   text NOT NULL DEFAULT '',
                    teacher       text NOT NULL DEFAULT '',
                    description   text NULL,
                    due_date_raw  text NULL,
                    due_date      timestamptz NULL,
                    course_id     text NULL,
                    item_id       text NULL,
                    message_date  timestamptz NOT NULL,
                    detected_at   timestamptz NOT NULL DEFAULT now()
                );

                CREATE INDEX IF NOT EXISTS ix_detected_assignments_student
                    ON inbox.detected_assignments (student_id);

                CREATE UNIQUE INDEX IF NOT EXISTS ix_detected_assignments_student_item
                    ON inbox.detected_assignments (student_id, item_id)
                    WHERE item_id IS NOT NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS inbox.detected_assignments;");
        }
    }
}
