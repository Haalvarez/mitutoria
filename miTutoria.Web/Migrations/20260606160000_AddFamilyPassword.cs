using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using miTutoria.Web.Data;

#nullable disable

namespace miTutoria.Web.Migrations
{
    /// <summary>
    /// Auth — contraseña del padre (login con email+password; magic link solo para crear/resetear).
    /// Idempotente (IF NOT EXISTS). null = la familia todavía no creó contraseña.
    /// Lleva [DbContext] + [Migration] para que db.Database.Migrate() la aplique sola en el arranque.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260606160000_AddFamilyPassword")]
    public partial class AddFamilyPassword : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE auth.families ADD COLUMN IF NOT EXISTS password_hash text NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE auth.families DROP COLUMN IF EXISTS password_hash;");
        }
    }
}
