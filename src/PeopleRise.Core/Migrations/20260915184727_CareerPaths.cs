using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleRise.Core.Migrations
{
    /// <inheritdoc />
    public partial class CareerPaths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "career_path_steps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    career_path_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grade_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_career_path_steps", x => x.id);
                    table.ForeignKey(
                        name: "FK_career_path_steps_grades_grade_id",
                        column: x => x.grade_id,
                        principalTable: "grades",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "career_paths",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name_en = table.Column<string>(type: "text", nullable: false),
                    name_ar = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_career_paths", x => x.id);
                    table.ForeignKey(
                        name: "FK_career_paths_job_families_job_family_id",
                        column: x => x.job_family_id,
                        principalTable: "job_families",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_career_path_steps_career_path_id_grade_id",
                table: "career_path_steps",
                columns: new[] { "career_path_id", "grade_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_career_path_steps_career_path_id_step_order",
                table: "career_path_steps",
                columns: new[] { "career_path_id", "step_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_career_path_steps_grade_id",
                table: "career_path_steps",
                column: "grade_id");

            migrationBuilder.CreateIndex(
                name: "IX_career_paths_job_family_id",
                table: "career_paths",
                column: "job_family_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "career_path_steps");

            migrationBuilder.DropTable(
                name: "career_paths");
        }
    }
}
