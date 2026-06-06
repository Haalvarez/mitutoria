using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using miTutoria.Web.Data;

#nullable disable

namespace miTutoria.Web.Migrations
{
    /// <summary>
    /// Cobro — flag por familia para mostrar el botón "Quiero pagar" (rollout/prueba sin habilitarlo a todos).
    /// Idempotente (IF NOT EXISTS). Default false.
    /// Lleva [DbContext] + [Migration] para que db.Database.Migrate() la aplique sola en el arranque.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260606150000_AddPayEnabled")]
    public partial class AddPayEnabled : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE auth.families ADD COLUMN IF NOT EXISTS pay_enabled boolean NOT NULL DEFAULT false;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE auth.families DROP COLUMN IF EXISTS pay_enabled;");
        }
    }
}
