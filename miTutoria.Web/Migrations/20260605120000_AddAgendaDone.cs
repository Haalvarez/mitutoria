using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using miTutoria.Web.Data;

#nullable disable

namespace miTutoria.Web.Migrations
{
    /// <summary>
    /// Track 2 — Capa 6: marcar tareas como hechas (el ✓ que da el micro-logro). Idempotente.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260605120000_AddAgendaDone")]
    public partial class AddAgendaDone : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE inbox.detected_assignments ADD COLUMN IF NOT EXISTS done boolean NOT NULL DEFAULT false;
                ALTER TABLE inbox.detected_assignments ADD COLUMN IF NOT EXISTS done_at timestamptz NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE inbox.detected_assignments DROP COLUMN IF EXISTS done;
                ALTER TABLE inbox.detected_assignments DROP COLUMN IF EXISTS done_at;
            ");
        }
    }
}
