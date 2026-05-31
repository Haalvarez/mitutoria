using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace miTutoria.Web.Migrations
{
    public partial class AddStudentProfile2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE auth.users
                    ADD COLUMN IF NOT EXISTS ""Gender""             text NOT NULL DEFAULT 'NoEspecificado',
                    ADD COLUMN IF NOT EXISTS ""ExplanationLevel""   text NOT NULL DEFAULT 'AcordeAlAño',
                    ADD COLUMN IF NOT EXISTS ""PrefShortMessages""  boolean NOT NULL DEFAULT false,
                    ADD COLUMN IF NOT EXISTS ""PrefVisualExamples"" boolean NOT NULL DEFAULT false,
                    ADD COLUMN IF NOT EXISTS ""PrefFrequentPraise"" boolean NOT NULL DEFAULT false,
                    ADD COLUMN IF NOT EXISTS ""PrefExtraPatience""  boolean NOT NULL DEFAULT false,
                    ADD COLUMN IF NOT EXISTS ""PrefSlowPace""       boolean NOT NULL DEFAULT false,
                    ADD COLUMN IF NOT EXISTS ""PrefOneQuestionOnly"" boolean NOT NULL DEFAULT false,
                    ADD COLUMN IF NOT EXISTS ""PrefRefocusReminder"" boolean NOT NULL DEFAULT false;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE auth.users
                    DROP COLUMN IF EXISTS ""Gender"",
                    DROP COLUMN IF EXISTS ""ExplanationLevel"",
                    DROP COLUMN IF EXISTS ""PrefShortMessages"",
                    DROP COLUMN IF EXISTS ""PrefVisualExamples"",
                    DROP COLUMN IF EXISTS ""PrefFrequentPraise"",
                    DROP COLUMN IF EXISTS ""PrefExtraPatience"",
                    DROP COLUMN IF EXISTS ""PrefSlowPace"",
                    DROP COLUMN IF EXISTS ""PrefOneQuestionOnly"",
                    DROP COLUMN IF EXISTS ""PrefRefocusReminder"";
            ");
        }
    }
}
