using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using miTutoria.Web.Data;

#nullable disable

namespace miTutoria.Web.Migrations
{
    /// <summary>
    /// Analítica de la landing — tabla de hits humanos a la home (public.landing_hits).
    /// Idempotente (IF NOT EXISTS). Lleva [DbContext] + [Migration] para que
    /// db.Database.Migrate() la aplique sola en el arranque.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260620120000_AddLandingHits")]
    public partial class AddLandingHits : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS public.landing_hits (
                    id          serial PRIMARY KEY,
                    created_at  timestamptz NOT NULL DEFAULT now(),
                    path        text,
                    referrer    text,
                    user_agent  text,
                    is_mobile   boolean NOT NULL DEFAULT false
                );

                CREATE INDEX IF NOT EXISTS ix_landing_hits_created_at
                    ON public.landing_hits (created_at);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS public.landing_hits;");
        }
    }
}
