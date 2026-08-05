using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleRise.Modules.JobReward.Migrations
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
                name: "market_data_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    source = table.Column<string>(type: "text", nullable: true),
                    effective_date = table.Column<DateOnly>(type: "date", nullable: false),
                    currency = table.Column<string>(type: "char(3)", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_market_data_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "methodologies",
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
                    table.PrimaryKey("PK_methodologies", x => x.id);
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
                name: "salary_import_batches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    filename = table.Column<string>(type: "text", nullable: true),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    row_count = table.Column<int>(type: "integer", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salary_import_batches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "band_positioning_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_family_id = table.Column<Guid>(type: "uuid", nullable: true),
                    posture = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    target_percentile = table.Column<int>(type: "integer", nullable: false),
                    effective_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_band_positioning_policies", x => x.id);
                    table.ForeignKey(
                        name: "FK_band_positioning_policies_job_families_job_family_id",
                        column: x => x.job_family_id,
                        principalTable: "job_families",
                        principalColumn: "id");
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
                name: "market_data_points",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_family_id = table.Column<Guid>(type: "uuid", nullable: true),
                    level_id = table.Column<Guid>(type: "uuid", nullable: true),
                    grade_id = table.Column<Guid>(type: "uuid", nullable: true),
                    geography = table.Column<string>(type: "text", nullable: true),
                    industry = table.Column<string>(type: "text", nullable: true),
                    company_size = table.Column<string>(type: "text", nullable: true),
                    currency = table.Column<string>(type: "char(3)", nullable: false),
                    p25 = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    p50 = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    p75 = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    p90 = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_market_data_points", x => x.id);
                    table.ForeignKey(
                        name: "FK_market_data_points_market_data_snapshots_snapshot_id",
                        column: x => x.snapshot_id,
                        principalTable: "market_data_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "methodology_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    methodology_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_no = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    min_points = table.Column<int>(type: "integer", nullable: false),
                    max_points = table.Column<int>(type: "integer", nullable: false),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_methodology_versions", x => x.id);
                    table.ForeignKey(
                        name: "FK_methodology_versions_methodologies_methodology_id",
                        column: x => x.methodology_id,
                        principalTable: "methodologies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_compensations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    base_salary = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    currency = table.Column<string>(type: "char(3)", nullable: false),
                    effective_date = table.Column<DateOnly>(type: "date", nullable: false),
                    import_batch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_compensations", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_compensations_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_compensations_salary_import_batches_import_batch_id",
                        column: x => x.import_batch_id,
                        principalTable: "salary_import_batches",
                        principalColumn: "id");
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
                name: "salary_bands",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    grade_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_family_id = table.Column<Guid>(type: "uuid", nullable: true),
                    currency = table.Column<string>(type: "char(3)", nullable: false),
                    midpoint = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    min_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    max_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    overlap_pct = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    source_snapshot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    positioning_id = table.Column<Guid>(type: "uuid", nullable: true),
                    effective_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salary_bands", x => x.id);
                    table.CheckConstraint("ck_band_order", "max_amount >= midpoint AND midpoint >= min_amount");
                    table.ForeignKey(
                        name: "FK_salary_bands_grades_grade_id",
                        column: x => x.grade_id,
                        principalTable: "grades",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_salary_bands_job_families_job_family_id",
                        column: x => x.job_family_id,
                        principalTable: "job_families",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "factors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    methodology_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    name_en = table.Column<string>(type: "text", nullable: false),
                    name_ar = table.Column<string>(type: "text", nullable: true),
                    help_text_en = table.Column<string>(type: "text", nullable: true),
                    help_text_ar = table.Column<string>(type: "text", nullable: true),
                    weight = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_factors", x => x.id);
                    table.ForeignKey(
                        name: "FK_factors_methodology_versions_methodology_version_id",
                        column: x => x.methodology_version_id,
                        principalTable: "methodology_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "grade_mappings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    methodology_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grade_id = table.Column<Guid>(type: "uuid", nullable: false),
                    min_score = table.Column<int>(type: "integer", nullable: true),
                    max_score = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grade_mappings", x => x.id);
                    table.CheckConstraint("ck_grade_mapping_score", "max_score >= min_score");
                    table.ForeignKey(
                        name: "FK_grade_mappings_grades_grade_id",
                        column: x => x.grade_id,
                        principalTable: "grades",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_grade_mappings_methodology_versions_methodology_version_id",
                        column: x => x.methodology_version_id,
                        principalTable: "methodology_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evaluations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    methodology_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evaluator_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    total_score = table.Column<int>(type: "integer", nullable: true),
                    recommended_grade_id = table.Column<Guid>(type: "uuid", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evaluations", x => x.id);
                    table.ForeignKey(
                        name: "FK_evaluations_employees_evaluator_employee_id",
                        column: x => x.evaluator_employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evaluations_grades_recommended_grade_id",
                        column: x => x.recommended_grade_id,
                        principalTable: "grades",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_evaluations_jobs_job_id",
                        column: x => x.job_id,
                        principalTable: "jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_evaluations_methodology_versions_methodology_version_id",
                        column: x => x.methodology_version_id,
                        principalTable: "methodology_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "questions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    factor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_text_en = table.Column<string>(type: "text", nullable: false),
                    question_text_ar = table.Column<string>(type: "text", nullable: true),
                    help_text_en = table.Column<string>(type: "text", nullable: true),
                    help_text_ar = table.Column<string>(type: "text", nullable: true),
                    question_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    weight = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_questions", x => x.id);
                    table.ForeignKey(
                        name: "FK_questions_factors_factor_id",
                        column: x => x.factor_id,
                        principalTable: "factors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evaluation_factor_scores",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    evaluation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    factor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evaluation_factor_scores", x => x.id);
                    table.ForeignKey(
                        name: "FK_evaluation_factor_scores_evaluations_evaluation_id",
                        column: x => x.evaluation_id,
                        principalTable: "evaluations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_evaluation_factor_scores_factors_factor_id",
                        column: x => x.factor_id,
                        principalTable: "factors",
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

            migrationBuilder.CreateTable(
                name: "answer_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label_en = table.Column<string>(type: "text", nullable: false),
                    label_ar = table.Column<string>(type: "text", nullable: true),
                    help_text_en = table.Column<string>(type: "text", nullable: true),
                    help_text_ar = table.Column<string>(type: "text", nullable: true),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_answer_options", x => x.id);
                    table.ForeignKey(
                        name: "FK_answer_options_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evaluation_answers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    evaluation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    answer_option_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating_snapshot = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evaluation_answers", x => x.id);
                    table.ForeignKey(
                        name: "FK_evaluation_answers_answer_options_answer_option_id",
                        column: x => x.answer_option_id,
                        principalTable: "answer_options",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_evaluation_answers_evaluations_evaluation_id",
                        column: x => x.evaluation_id,
                        principalTable: "evaluations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_evaluation_answers_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_answer_options_question_id",
                table: "answer_options",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_band_positioning_policies_job_family_id",
                table: "band_positioning_policies",
                column: "job_family_id");

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
                name: "IX_employee_compensations_employee_id",
                table: "employee_compensations",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_compensations_import_batch_id",
                table: "employee_compensations",
                column: "import_batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_employee_no",
                table: "employees",
                column: "employee_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_evaluation_answers_answer_option_id",
                table: "evaluation_answers",
                column: "answer_option_id");

            migrationBuilder.CreateIndex(
                name: "IX_evaluation_answers_evaluation_id_question_id_answer_option_~",
                table: "evaluation_answers",
                columns: new[] { "evaluation_id", "question_id", "answer_option_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_evaluation_answers_question_id",
                table: "evaluation_answers",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_evaluation_factor_scores_evaluation_id",
                table: "evaluation_factor_scores",
                column: "evaluation_id");

            migrationBuilder.CreateIndex(
                name: "IX_evaluation_factor_scores_factor_id",
                table: "evaluation_factor_scores",
                column: "factor_id");

            migrationBuilder.CreateIndex(
                name: "IX_evaluations_evaluator_employee_id",
                table: "evaluations",
                column: "evaluator_employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_evaluations_job_id",
                table: "evaluations",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "IX_evaluations_methodology_version_id",
                table: "evaluations",
                column: "methodology_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_evaluations_recommended_grade_id",
                table: "evaluations",
                column: "recommended_grade_id");

            migrationBuilder.CreateIndex(
                name: "IX_factors_methodology_version_id",
                table: "factors",
                column: "methodology_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_grade_mappings_grade_id",
                table: "grade_mappings",
                column: "grade_id");

            migrationBuilder.CreateIndex(
                name: "IX_grade_mappings_methodology_version_id_grade_id",
                table: "grade_mappings",
                columns: new[] { "methodology_version_id", "grade_id" },
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
                name: "IX_market_data_points_snapshot_id",
                table: "market_data_points",
                column: "snapshot_id");

            migrationBuilder.CreateIndex(
                name: "IX_methodology_versions_methodology_id_version_no",
                table: "methodology_versions",
                columns: new[] { "methodology_id", "version_no" },
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

            migrationBuilder.CreateIndex(
                name: "IX_questions_factor_id",
                table: "questions",
                column: "factor_id");

            migrationBuilder.CreateIndex(
                name: "IX_salary_bands_grade_id",
                table: "salary_bands",
                column: "grade_id");

            migrationBuilder.CreateIndex(
                name: "IX_salary_bands_job_family_id",
                table: "salary_bands",
                column: "job_family_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "band_positioning_policies");

            migrationBuilder.DropTable(
                name: "employee_assignments");

            migrationBuilder.DropTable(
                name: "employee_compensations");

            migrationBuilder.DropTable(
                name: "evaluation_answers");

            migrationBuilder.DropTable(
                name: "evaluation_factor_scores");

            migrationBuilder.DropTable(
                name: "grade_mappings");

            migrationBuilder.DropTable(
                name: "market_data_points");

            migrationBuilder.DropTable(
                name: "salary_bands");

            migrationBuilder.DropTable(
                name: "job_positions");

            migrationBuilder.DropTable(
                name: "salary_import_batches");

            migrationBuilder.DropTable(
                name: "answer_options");

            migrationBuilder.DropTable(
                name: "evaluations");

            migrationBuilder.DropTable(
                name: "market_data_snapshots");

            migrationBuilder.DropTable(
                name: "org_units");

            migrationBuilder.DropTable(
                name: "questions");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "jobs");

            migrationBuilder.DropTable(
                name: "factors");

            migrationBuilder.DropTable(
                name: "grades");

            migrationBuilder.DropTable(
                name: "job_families");

            migrationBuilder.DropTable(
                name: "methodology_versions");

            migrationBuilder.DropTable(
                name: "levels");

            migrationBuilder.DropTable(
                name: "methodologies");
        }
    }
}
