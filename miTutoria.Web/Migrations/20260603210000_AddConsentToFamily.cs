using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace miTutoria.Web.Migrations;

/// <summary>
/// Agrega consent_at, consent_ip, consent_version a auth.families.
/// Aplicar manualmente en TablePlus antes de hacer push.
/// </summary>
public partial class AddConsentToFamily : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            ALTER TABLE auth.families ADD COLUMN IF NOT EXISTS consent_at timestamptz;
            ALTER TABLE auth.families ADD COLUMN IF NOT EXISTS consent_ip text;
            ALTER TABLE auth.families ADD COLUMN IF NOT EXISTS consent_version text;
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            ALTER TABLE auth.families DROP COLUMN IF EXISTS consent_at;
            ALTER TABLE auth.families DROP COLUMN IF EXISTS consent_ip;
            ALTER TABLE auth.families DROP COLUMN IF EXISTS consent_version;
        ");
    }
}
