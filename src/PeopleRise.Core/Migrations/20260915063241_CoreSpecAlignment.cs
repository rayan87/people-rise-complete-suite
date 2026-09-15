using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleRise.Core.Migrations
{
    /// <inheritdoc />
    public partial class CoreSpecAlignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_jobs_grades_grade_id",
                table: "jobs");

            migrationBuilder.DropIndex(
                name: "IX_jobs_grade_id",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "grade_id",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "grade_source",
                table: "jobs");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "levels",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "job_families",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "grades",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.CreateTable(
                name: "job_grade_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grade_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    effective_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_grade_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_job_grade_assignments_grades_grade_id",
                        column: x => x.grade_id,
                        principalTable: "grades",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_job_grade_assignments_jobs_job_id",
                        column: x => x.job_id,
                        principalTable: "jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_salary_bands_grade_family_published",
                table: "salary_bands",
                columns: new[] { "grade_id", "job_family_id" },
                unique: true,
                filter: "status = 'Published' AND job_family_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_salary_bands_grade_no_family_published",
                table: "salary_bands",
                column: "grade_id",
                unique: true,
                filter: "status = 'Published' AND job_family_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_employee_pay_element_amounts_employee_id_pay_element_id",
                table: "employee_pay_element_amounts",
                columns: new[] { "employee_id", "pay_element_id" },
                unique: true,
                filter: "end_date IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_employee_pay_element_amounts_pay_element_id",
                table: "employee_pay_element_amounts",
                column: "pay_element_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_grade_assignments_grade_id",
                table: "job_grade_assignments",
                column: "grade_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_grade_assignments_job_id",
                table: "job_grade_assignments",
                column: "job_id",
                unique: true,
                filter: "end_date IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_employee_pay_element_amounts_pay_elements_pay_element_id",
                table: "employee_pay_element_amounts",
                column: "pay_element_id",
                principalTable: "pay_elements",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_employee_pay_element_amounts_pay_elements_pay_element_id",
                table: "employee_pay_element_amounts");

            migrationBuilder.DropTable(
                name: "job_grade_assignments");

            migrationBuilder.DropIndex(
                name: "ix_salary_bands_grade_family_published",
                table: "salary_bands");

            migrationBuilder.DropIndex(
                name: "ix_salary_bands_grade_no_family_published",
                table: "salary_bands");

            migrationBuilder.DropIndex(
                name: "IX_employee_pay_element_amounts_employee_id_pay_element_id",
                table: "employee_pay_element_amounts");

            migrationBuilder.DropIndex(
                name: "IX_employee_pay_element_amounts_pay_element_id",
                table: "employee_pay_element_amounts");

            migrationBuilder.DropColumn(
                name: "status",
                table: "levels");

            migrationBuilder.DropColumn(
                name: "status",
                table: "job_families");

            migrationBuilder.DropColumn(
                name: "status",
                table: "grades");

            migrationBuilder.AddColumn<Guid>(
                name: "grade_id",
                table: "jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "grade_source",
                table: "jobs",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_jobs_grade_id",
                table: "jobs",
                column: "grade_id");

            migrationBuilder.AddForeignKey(
                name: "FK_jobs_grades_grade_id",
                table: "jobs",
                column: "grade_id",
                principalTable: "grades",
                principalColumn: "id");
        }
    }
}
