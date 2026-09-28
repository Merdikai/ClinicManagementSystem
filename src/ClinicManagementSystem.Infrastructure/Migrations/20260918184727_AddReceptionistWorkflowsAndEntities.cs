using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReceptionistWorkflowsAndEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InsuranceCoveragePlan",
                table: "Patients",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsuranceEligibilityStatus",
                table: "Patients",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsurancePolicyNumber",
                table: "Patients",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsuranceProvider",
                table: "Patients",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NationalId",
                table: "Patients",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TokenNumber",
                table: "Appointments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VisitType",
                table: "Appointments",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DailyCashReconciliations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceptionistUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReconciliationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Shift = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CashCollected = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CardCollected = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DigitalCollected = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalCollected = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ClosingCashCounted = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscrepancyAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    InvoicesSettledCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyCashReconciliations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyCashReconciliations_Users_ReceptionistUserId",
                        column: x => x.ReceptionistUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VisitorInquiries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InquiryType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DepartmentTarget = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    HandledByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitorInquiries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisitorInquiries_Users_HandledByUserId",
                        column: x => x.HandledByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyCashReconciliations_ReceptionistUserId",
                table: "DailyCashReconciliations",
                column: "ReceptionistUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorInquiries_HandledByUserId",
                table: "VisitorInquiries",
                column: "HandledByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyCashReconciliations");

            migrationBuilder.DropTable(
                name: "VisitorInquiries");

            migrationBuilder.DropColumn(
                name: "InsuranceCoveragePlan",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "InsuranceEligibilityStatus",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "InsurancePolicyNumber",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "InsuranceProvider",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "NationalId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "TokenNumber",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "VisitType",
                table: "Appointments");
        }
    }
}
