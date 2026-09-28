using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNurseWorkflowsAndEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OxygenSaturation",
                table: "VitalSigns",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PainScore",
                table: "VitalSigns",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoomAssignment",
                table: "Appointments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TriageNotes",
                table: "Appointments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TriageUrgency",
                table: "Appointments",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NursingProcedures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    PerformedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcedureType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    BodySite = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PatientTolerance = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SuppliesUsed = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NursingProcedures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NursingProcedures_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_NursingProcedures_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NursingProcedures_Users_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShiftHandovers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NurseUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShiftType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Situation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Background = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Assessment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Recommendation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ActivePatientCount = table.Column<int>(type: "integer", nullable: false),
                    CriticalAlertsCount = table.Column<int>(type: "integer", nullable: false),
                    HandoverTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftHandovers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftHandovers_Users_NurseUserId",
                        column: x => x.NurseUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NursingProcedures_AppointmentId",
                table: "NursingProcedures",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_NursingProcedures_PatientId",
                table: "NursingProcedures",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_NursingProcedures_PerformedByUserId",
                table: "NursingProcedures",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftHandovers_NurseUserId",
                table: "ShiftHandovers",
                column: "NurseUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NursingProcedures");

            migrationBuilder.DropTable(
                name: "ShiftHandovers");

            migrationBuilder.DropColumn(
                name: "OxygenSaturation",
                table: "VitalSigns");

            migrationBuilder.DropColumn(
                name: "PainScore",
                table: "VitalSigns");

            migrationBuilder.DropColumn(
                name: "RoomAssignment",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "TriageNotes",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "TriageUrgency",
                table: "Appointments");
        }
    }
}
