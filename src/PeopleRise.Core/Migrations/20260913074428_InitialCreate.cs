using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleRise.Core.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_no = table.Column<string>(type: "text", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "job_families",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    name_en = table.Column<string>(type: "text", nullable: false),
                    name_ar = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_families", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "levels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    name_en = table.Column<string>(type: "text", nullable: false),
                    name_ar = table.Column<string>(type: "text", nullable: true),
                    rank = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_levels", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "org_units",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_org_units", x => x.id);
                    table.ForeignKey(
                        name: "FK_org_units_org_units_parent_id",
                        column: x => x.parent_id,
                        principalTable: "org_units",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    legal_name_en = table.Column<string>(type: "text", nullable: false),
                    legal_name_ar = table.Column<string>(type: "text", nullable: true),
                    trade_name_en = table.Column<string>(type: "text", nullable: true),
                    trade_name_ar = table.Column<string>(type: "text", nullable: true),
                    industry_code = table.Column<string>(type: "text", nullable: true),
                    sector = table.Column<string>(type: "text", nullable: true),
                    base_currency = table.Column<string>(type: "text", nullable: false),
                    fiscal_year_start_month = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "grades",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    name_en = table.Column<string>(type: "text", nullable: false),
                    name_ar = table.Column<string>(type: "text", nullable: true),
                    rank = table.Column<int>(type: "integer", nullable: false),
                    level_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grades", x => x.id);
                    table.ForeignKey(
                        name: "FK_grades_levels_level_id",
                        column: x => x.level_id,
                        principalTable: "levels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    title_en = table.Column<string>(type: "text", nullable: false),
                    title_ar = table.Column<string>(type: "text", nullable: true),
                    description_en = table.Column<string>(type: "text", nullable: true),
                    description_ar = table.Column<string>(type: "text", nullable: true),
                    job_family_id = table.Column<Guid>(type: "uuid", nullable: true),
                    grade_id = table.Column<Guid>(type: "uuid", nullable: true),
                    grade_source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_jobs", x => x.id);
                    table.ForeignKey(
                        name: "FK_jobs_grades_grade_id",
                        column: x => x.grade_id,
                        principalTable: "grades",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_jobs_job_families_job_family_id",
                        column: x => x.job_family_id,
                        principalTable: "job_families",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "job_positions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_positions", x => x.id);
                    table.ForeignKey(
                        name: "FK_job_positions_jobs_job_id",
                        column: x => x.job_id,
                        principalTable: "jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_job_positions_org_units_org_unit_id",
                        column: x => x.org_unit_id,
                        principalTable: "org_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_assignments_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_assignments_job_positions_position_id",
                        column: x => x.position_id,
                        principalTable: "job_positions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_employee_assignments_employee_id",
                table: "employee_assignments",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_assignments_position_id",
                table: "employee_assignments",
                column: "position_id",
                unique: true,
                filter: "end_date IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_employees_employee_no",
                table: "employees",
                column: "employee_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_grades_code",
                table: "grades",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_grades_level_id",
                table: "grades",
                column: "level_id");

            migrationBuilder.CreateIndex(
                name: "IX_grades_rank",
                table: "grades",
                column: "rank",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_families_code",
                table: "job_families",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_positions_code",
                table: "job_positions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_positions_job_id",
                table: "job_positions",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_positions_org_unit_id",
                table: "job_positions",
                column: "org_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_jobs_code",
                table: "jobs",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_jobs_grade_id",
                table: "jobs",
                column: "grade_id");

            migrationBuilder.CreateIndex(
                name: "IX_jobs_job_family_id",
                table: "jobs",
                column: "job_family_id");

            migrationBuilder.CreateIndex(
                name: "IX_levels_code",
                table: "levels",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_levels_rank",
                table: "levels",
                column: "rank",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_org_units_code",
                table: "org_units",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_org_units_parent_id",
                table: "org_units",
                column: "parent_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_assignments");

            migrationBuilder.DropTable(
                name: "organizations");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "job_positions");

            migrationBuilder.DropTable(
                name: "jobs");

            migrationBuilder.DropTable(
                name: "org_units");

            migrationBuilder.DropTable(
                name: "grades");

            migrationBuilder.DropTable(
                name: "job_families");

            migrationBuilder.DropTable(
                name: "levels");
        }
    }
}
