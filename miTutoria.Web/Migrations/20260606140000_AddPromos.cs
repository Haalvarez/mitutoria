using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using miTutoria.Web.Data;

#nullable disable

namespace miTutoria.Web.Migrations
{
    /// <summary>
    /// Cobro — cupones de descuento (familia y amigos) + columna promo_code en payments.
    /// Idempotente (IF NOT EXISTS). Schema 'billing'. Clave única.
    /// Lleva [DbContext] + [Migration] para que db.Database.Migrate() la aplique sola en el arranque.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260606140000_AddPromos")]
    public partial class AddPromos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE SCHEMA IF NOT EXISTS billing;

                CREATE TABLE IF NOT EXISTS billing.promos (
                    id          serial PRIMARY KEY,
                    code        text NOT NULL,
                    name        text NOT NULL DEFAULT '',
                    amount_ars  numeric NOT NULL DEFAULT 0,
                    valid_from  timestamptz NULL,
                    valid_until timestamptz NULL,
                    active      boolean NOT NULL DEFAULT true,
                    max_uses    integer NULL,
                    used_count  integer NOT NULL DEFAULT 0,
                    created_at  timestamptz NOT NULL DEFAULT now()
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_promos_code ON billing.promos (code);

                ALTER TABLE billing.payments ADD COLUMN IF NOT EXISTS promo_code text NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE billing.payments DROP COLUMN IF EXISTS promo_code;
                DROP TABLE IF EXISTS billing.promos;
            ");
        }
    }
}
