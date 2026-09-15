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
                        name: "FK_evaluations_methodology_versions_methodology_version_id",
                        column: x => x.methodology_version_id,
                        principalTable: "methodology_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
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
                        name: "FK_grade_mappings_methodology_versions_methodology_version_id",
                        column: x => x.methodology_version_id,
                        principalTable: "methodology_versions",
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
                name: "IX_evaluations_methodology_version_id",
                table: "evaluations",
                column: "methodology_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_factors_methodology_version_id",
                table: "factors",
                column: "methodology_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_grade_mappings_methodology_version_id_grade_id",
                table: "grade_mappings",
                columns: new[] { "methodology_version_id", "grade_id" },
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
                name: "IX_questions_factor_id",
                table: "questions",
                column: "factor_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "band_positioning_policies");

            migrationBuilder.DropTable(
                name: "evaluation_answers");

            migrationBuilder.DropTable(
                name: "evaluation_factor_scores");

            migrationBuilder.DropTable(
                name: "grade_mappings");

            migrationBuilder.DropTable(
                name: "market_data_points");

            migrationBuilder.DropTable(
                name: "answer_options");

            migrationBuilder.DropTable(
                name: "evaluations");

            migrationBuilder.DropTable(
                name: "market_data_snapshots");

            migrationBuilder.DropTable(
                name: "questions");

            migrationBuilder.DropTable(
                name: "factors");

            migrationBuilder.DropTable(
                name: "methodology_versions");

            migrationBuilder.DropTable(
                name: "methodologies");
        }
    }
}
