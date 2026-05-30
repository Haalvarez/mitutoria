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
            migrationBuilder.DropForeignKey(
                name: "FK_classrooms_subjects_SubjectId",
                schema: "academic",
                table: "classrooms");

            migrationBuilder.AlterColumn<int>(
                name: "SubjectId",
                schema: "academic",
                table: "classrooms",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_classrooms_subjects_SubjectId",
                schema: "academic",
                table: "classrooms",
                column: "SubjectId",
                principalSchema: "academic",
                principalTable: "subjects",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_classrooms_subjects_SubjectId",
                schema: "academic",
                table: "classrooms");

            migrationBuilder.AlterColumn<int>(
                name: "SubjectId",
                schema: "academic",
                table: "classrooms",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true,
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_classrooms_subjects_SubjectId",
                schema: "academic",
                table: "classrooms",
                column: "SubjectId",
                principalSchema: "academic",
                principalTable: "subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
