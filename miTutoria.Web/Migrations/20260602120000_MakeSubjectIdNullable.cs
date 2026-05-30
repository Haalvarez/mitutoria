using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace miTutoria.Web.Migrations
{
    /// <inheritdoc />
    public partial class MakeSubjectIdNullable2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE academic.classrooms DROP CONSTRAINT IF EXISTS ""FK_classrooms_subjects_SubjectId"";
                ALTER TABLE academic.classrooms ALTER COLUMN ""SubjectId"" DROP NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE academic.classrooms ALTER COLUMN ""SubjectId"" SET NOT NULL;
                ALTER TABLE academic.classrooms ADD CONSTRAINT ""FK_classrooms_subjects_SubjectId""
                    FOREIGN KEY (""SubjectId"") REFERENCES academic.subjects(""Id"") ON DELETE CASCADE;
            ");
        }
    }
}
