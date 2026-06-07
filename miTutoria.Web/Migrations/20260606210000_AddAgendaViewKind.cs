using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using miTutoria.Web.Data;

#nullable disable

namespace miTutoria.Web.Migrations
{
    /// <summary>
    /// Embudo del calendario público: distingue "view" (abrió) de "cta" (clickeó → landing).
    /// Idempotente (IF NOT EXISTS). Default 'view' (lo ya registrado eran visitas).
    /// Lleva [DbContext] + [Migration] para que db.Database.Migrate() la aplique sola en el arranque.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260606210000_AddAgendaViewKind")]
    public partial class AddAgendaViewKind : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE academic.agenda_views ADD COLUMN IF NOT EXISTS kind text NOT NULL DEFAULT 'view';
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE academic.agenda_views DROP COLUMN IF EXISTS kind;");
        }
    }
}
