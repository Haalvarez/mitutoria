using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using miTutoria.Web.Data;

#nullable disable

namespace miTutoria.Web.Migrations
{
    /// <summary>
    /// Cobro — tabla de pagos de la cuota mensual (MercadoPago Checkout Pro).
    /// Idempotente (IF NOT EXISTS). Schema 'billing'. Idempotencia del webhook por mp_payment_id.
    /// Lleva [DbContext] + [Migration] para que db.Database.Migrate() la aplique sola en el arranque.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260606120000_AddPayments")]
    public partial class AddPayments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE SCHEMA IF NOT EXISTS billing;

                CREATE TABLE IF NOT EXISTS billing.payments (
                    id            serial PRIMARY KEY,
                    family_id     integer NOT NULL,
                    preference_id text NULL,
                    mp_payment_id text NULL,
                    cycle_marker  text NULL,
                    amount_ars    numeric NOT NULL DEFAULT 0,
                    status        text NOT NULL DEFAULT 'pending',
                    created_at    timestamptz NOT NULL DEFAULT now(),
                    paid_at       timestamptz NULL
                );

                CREATE INDEX IF NOT EXISTS ix_payments_mp_payment_id
                    ON billing.payments (mp_payment_id);
                CREATE INDEX IF NOT EXISTS ix_payments_family_id
                    ON billing.payments (family_id);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS billing.payments;");
        }
    }
}
