using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using miTutoria.Web.Data;

#nullable disable

namespace miTutoria.Web.Migrations
{
    /// <summary>
    /// Podcast (audio-resumen de 2 hosts sobre el material):
    /// - flag por familia auth.families.podcast_enabled (rollout/prueba, default false)
    /// - tabla academic.podcast_episodes (audio WAV en bytea)
    /// Idempotente (IF NOT EXISTS). Lleva [DbContext] + [Migration] para que
    /// db.Database.Migrate() la aplique sola en el arranque.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260801203252_AddPodcast")]
    public partial class AddPodcast : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE auth.families ADD COLUMN IF NOT EXISTS podcast_enabled boolean NOT NULL DEFAULT false;

                CREATE TABLE IF NOT EXISTS academic.podcast_episodes (
                    id            serial PRIMARY KEY,
                    student_id    integer NOT NULL,
                    classroom_id  integer NOT NULL,
                    title         text NOT NULL DEFAULT '',
                    audio         bytea NOT NULL,
                    mime          text NOT NULL DEFAULT 'audio/wav',
                    duration_sec  integer NOT NULL DEFAULT 0,
                    created_at    timestamptz NOT NULL DEFAULT now()
                );

                CREATE INDEX IF NOT EXISTS ix_podcast_episodes_student_id
                    ON academic.podcast_episodes (student_id);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS academic.podcast_episodes;
                ALTER TABLE auth.families DROP COLUMN IF EXISTS podcast_enabled;
            ");
        }
    }
}
