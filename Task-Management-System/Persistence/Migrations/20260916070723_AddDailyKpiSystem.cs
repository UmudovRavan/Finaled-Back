using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyKpiSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PerformancePoints");

            migrationBuilder.AddColumn<Guid>(
                name: "DivisionId",
                table: "AppUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DailyKpiRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    DivisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    EvaluationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    JobDutiesScore = table.Column<int>(type: "integer", nullable: false),
                    DisciplineScore = table.Column<int>(type: "integer", nullable: false),
                    BonusScore = table.Column<int>(type: "integer", nullable: false),
                    TotalScore = table.Column<int>(type: "integer", nullable: false),
                    DisciplinePenaltyReason = table.Column<string>(type: "text", nullable: true),
                    BonusReason = table.Column<string>(type: "text", nullable: true),
                    Comments = table.Column<string>(type: "text", nullable: true),
                    IsAdminEdited = table.Column<bool>(type: "boolean", nullable: false),
                    AdminEditReason = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyKpiRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyKpiRecords_AppUsers_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyKpiRecords_AppUsers_EvaluatorId",
                        column: x => x.EvaluatorId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyKpiRecords_Divisions_DivisionId",
                        column: x => x.DivisionId,
                        principalTable: "Divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_DivisionId",
                table: "AppUsers",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyKpiRecords_DivisionId",
                table: "DailyKpiRecords",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyKpiRecords_EmployeeId",
                table: "DailyKpiRecords",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyKpiRecords_EvaluatorId",
                table: "DailyKpiRecords",
                column: "EvaluatorId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyKpiRecords_TenantId_EmployeeId_EvaluationDate",
                table: "DailyKpiRecords",
                columns: new[] { "TenantId", "EmployeeId", "EvaluationDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyKpiRecords_TenantId_EvaluationDate",
                table: "DailyKpiRecords",
                columns: new[] { "TenantId", "EvaluationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_AppUsers_Divisions_DivisionId",
                table: "AppUsers",
                column: "DivisionId",
                principalTable: "Divisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppUsers_Divisions_DivisionId",
                table: "AppUsers");

            migrationBuilder.DropTable(
                name: "DailyKpiRecords");

            migrationBuilder.DropIndex(
                name: "IX_AppUsers_DivisionId",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "DivisionId",
                table: "AppUsers");

            migrationBuilder.CreateTable(
                name: "PerformancePoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformancePoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformancePoints_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PerformancePoints_UserId",
                table: "PerformancePoints",
                column: "UserId");
        }
    }
}
