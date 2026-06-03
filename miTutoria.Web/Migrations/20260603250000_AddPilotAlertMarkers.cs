using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace miTutoria.Web.Migrations;

/// <summary>
/// Dedup de alertas del scheduler de piloto (costo y renovación) por ciclo de familia.
/// Idempotente — corre sola en el arranque.
/// </summary>
public partial class AddPilotAlertMarkers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            ALTER TABLE auth.families ADD COLUMN IF NOT EXISTS cost_alert_marker text;
            ALTER TABLE auth.families ADD COLUMN IF NOT EXISTS renewal_alert_marker text;
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            ALTER TABLE auth.families DROP COLUMN IF EXISTS cost_alert_marker;
            ALTER TABLE auth.families DROP COLUMN IF EXISTS renewal_alert_marker;
        ");
    }
}
