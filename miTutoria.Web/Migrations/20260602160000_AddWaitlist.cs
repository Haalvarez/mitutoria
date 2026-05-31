using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace miTutoria.Web.Migrations
{
    public partial class AddWaitlist : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS auth.waitlist_entries (
                    ""Id""        serial PRIMARY KEY,
                    ""Email""     text NOT NULL,
                    ""Name""      text NULL,
                    ""CreatedAt"" timestamptz NOT NULL DEFAULT now()
                );
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS auth.waitlist_entries;");
        }
    }
}
