using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace miTutoria.Web.Migrations
{
    public partial class AddArsRateToTokenEvent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE billing.token_events
                ADD COLUMN IF NOT EXISTS ""ArsRate"" numeric(12,4) NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE billing.token_events
                DROP COLUMN IF EXISTS ""ArsRate"";
            ");
        }
    }
}
