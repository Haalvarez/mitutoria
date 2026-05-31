using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace miTutoria.Web.Migrations
{
    public partial class AddStudentUsername : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE auth.users
                ADD COLUMN IF NOT EXISTS ""StudentUsername"" text NULL;

                CREATE UNIQUE INDEX IF NOT EXISTS ix_users_student_username
                ON auth.users (""StudentUsername"")
                WHERE ""StudentUsername"" IS NOT NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS auth.ix_users_student_username;
                ALTER TABLE auth.users DROP COLUMN IF EXISTS ""StudentUsername"";
            ");
        }
    }
}
