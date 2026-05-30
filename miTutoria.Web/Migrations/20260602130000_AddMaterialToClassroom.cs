using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace miTutoria.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialToClassroom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE academic.classrooms ADD COLUMN IF NOT EXISTS ""Material"" text NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE academic.classrooms DROP COLUMN IF EXISTS ""Material"";
            ");
        }
    }
}
