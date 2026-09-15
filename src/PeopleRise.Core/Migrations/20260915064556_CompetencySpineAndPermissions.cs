using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleRise.Core.Migrations
{
    /// <inheritdoc />
    public partial class CompetencySpineAndPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "competency_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    name_en = table.Column<string>(type: "text", nullable: false),
                    name_ar = table.Column<string>(type: "text", nullable: true),
                    description_en = table.Column<string>(type: "text", nullable: true),
                    description_ar = table.Column<string>(type: "text", nullable: true),
                    provenance = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competency_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "competency_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    level_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_family_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competency_templates", x => x.id);
                    table.ForeignKey(
                        name: "FK_competency_templates_job_families_job_family_id",
                        column: x => x.job_family_id,
                        principalTable: "job_families",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_competency_templates_levels_level_id",
                        column: x => x.level_id,
                        principalTable: "levels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name_en = table.Column<string>(type: "text", nullable: false),
                    name_ar = table.Column<string>(type: "text", nullable: true),
                    permissions_csv = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "certifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    competency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name_en = table.Column<string>(type: "text", nullable: false),
                    name_ar = table.Column<string>(type: "text", nullable: true),
                    level = table.Column<int>(type: "integer", nullable: false),
                    issued_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_certifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_certifications_competency_definitions_competency_id",
                        column: x => x.competency_id,
                        principalTable: "competency_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "competency_template_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    competency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    required_level = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competency_template_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_competency_template_items_competency_definitions_competency~",
                        column: x => x.competency_id,
                        principalTable: "competency_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "held_competency_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    competency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_held_competency_profiles", x => x.id);
                    table.ForeignKey(
                        name: "FK_held_competency_profiles_competency_definitions_competency_~",
                        column: x => x.competency_id,
                        principalTable: "competency_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "required_competency_overrides",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    competency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    required_level = table.Column<int>(type: "integer", nullable: true),
                    authored_framework_version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_required_competency_overrides", x => x.id);
                    table.ForeignKey(
                        name: "FK_required_competency_overrides_competency_definitions_compet~",
                        column: x => x.competency_id,
                        principalTable: "competency_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_role_assignments_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_certifications_competency_id",
                table: "certifications",
                column: "competency_id");

            migrationBuilder.CreateIndex(
                name: "IX_competency_definitions_code",
                table: "competency_definitions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_competency_template_items_competency_id",
                table: "competency_template_items",
                column: "competency_id");

            migrationBuilder.CreateIndex(
                name: "IX_competency_template_items_template_id_competency_id",
                table: "competency_template_items",
                columns: new[] { "template_id", "competency_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_competency_templates_job_family_id",
                table: "competency_templates",
                column: "job_family_id");

            migrationBuilder.CreateIndex(
                name: "ix_competency_templates_level_family",
                table: "competency_templates",
                columns: new[] { "level_id", "job_family_id" },
                unique: true,
                filter: "job_family_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_competency_templates_level_no_family",
                table: "competency_templates",
                column: "level_id",
                unique: true,
                filter: "job_family_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_held_competency_profiles_competency_id",
                table: "held_competency_profiles",
                column: "competency_id");

            migrationBuilder.CreateIndex(
                name: "IX_required_competency_overrides_competency_id",
                table: "required_competency_overrides",
                column: "competency_id");

            migrationBuilder.CreateIndex(
                name: "IX_required_competency_overrides_job_id_competency_id",
                table: "required_competency_overrides",
                columns: new[] { "job_id", "competency_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_assignments_role_id",
                table: "role_assignments",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_assignments_user_id_role_id",
                table: "role_assignments",
                columns: new[] { "user_id", "role_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "certifications");

            migrationBuilder.DropTable(
                name: "competency_template_items");

            migrationBuilder.DropTable(
                name: "competency_templates");

            migrationBuilder.DropTable(
                name: "held_competency_profiles");

            migrationBuilder.DropTable(
                name: "required_competency_overrides");

            migrationBuilder.DropTable(
                name: "role_assignments");

            migrationBuilder.DropTable(
                name: "competency_definitions");

            migrationBuilder.DropTable(
                name: "roles");
        }
    }
}
