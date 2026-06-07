using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using miTutoria.Web.Data;

#nullable disable

namespace miTutoria.Web.Migrations
{
    /// <summary>
    /// Digest de Telegram: marcador de "ya avisado" por assignment (evita repetir en cada pasada).
    /// Idempotente (IF NOT EXISTS).
    /// Lleva [DbContext] + [Migration] para que db.Database.Migrate() la aplique sola en el arranque.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260606190000_AddAssignmentNotifiedAt")]
    public partial class AddAssignmentNotifiedAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE inbox.detected_assignments ADD COLUMN IF NOT EXISTS notified_at timestamptz NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE inbox.detected_assignments DROP COLUMN IF EXISTS notified_at;");
        }
    }
}
