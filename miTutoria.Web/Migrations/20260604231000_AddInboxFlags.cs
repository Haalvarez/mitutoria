using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using miTutoria.Web.Data;

#nullable disable

namespace miTutoria.Web.Migrations
{
    /// <summary>
    /// Track 2 — Capa 0: flag de rollout por familia (inbox_enabled) y casilla
    /// del colegio del alumno (classroom_email) para mapear el "to" de los mails.
    /// Idempotente. [DbContext]+[Migration] para que Migrate() la aplique sola.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260604231000_AddInboxFlags")]
    public partial class AddInboxFlags : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE auth.families ADD COLUMN IF NOT EXISTS inbox_enabled boolean NOT NULL DEFAULT false;
                ALTER TABLE auth.users    ADD COLUMN IF NOT EXISTS classroom_email text NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE auth.families DROP COLUMN IF EXISTS inbox_enabled;
                ALTER TABLE auth.users    DROP COLUMN IF EXISTS classroom_email;
            ");
        }
    }
}
