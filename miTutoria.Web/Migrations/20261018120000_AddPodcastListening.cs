using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using miTutoria.Web.Data;

#nullable disable

namespace miTutoria.Web.Migrations
{
    /// <summary>
    /// Adopción del podcast: primer play y % máximo escuchado por episodio
    /// (lo reporta el reproductor). Idempotente (IF NOT EXISTS). Lleva [DbContext] + [Migration]
    /// para que db.Database.Migrate() la aplique sola en el arranque.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261018120000_AddPodcastListening")]
    public partial class AddPodcastListening : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE academic.podcast_episodes
                    ADD COLUMN IF NOT EXISTS first_played_at timestamptz NULL,
                    ADD COLUMN IF NOT EXISTS max_progress_pct integer NOT NULL DEFAULT 0;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE academic.podcast_episodes
                    DROP COLUMN IF EXISTS first_played_at,
                    DROP COLUMN IF EXISTS max_progress_pct;
            ");
        }
    }
}
