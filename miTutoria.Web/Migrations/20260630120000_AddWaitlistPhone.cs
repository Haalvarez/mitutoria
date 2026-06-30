using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using miTutoria.Web.Data;

#nullable disable

namespace miTutoria.Web.Migrations
{
    /// <summary>
    /// Agrega "Phone" (WhatsApp opcional) a auth.waitlist_entries.
    /// Idempotente (IF NOT EXISTS). Lleva [DbContext] + [Migration] para que
    /// db.Database.Migrate() la aplique sola en el arranque (sin TablePlus).
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260630120000_AddWaitlistPhone")]
    public partial class AddWaitlistPhone : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE auth.waitlist_entries
                    ADD COLUMN IF NOT EXISTS ""Phone"" text NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE auth.waitlist_entries
                    DROP COLUMN IF EXISTS ""Phone"";
            ");
        }
    }
}
