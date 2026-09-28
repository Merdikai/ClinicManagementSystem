using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRadiologyAndClinicalDoctorEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "PerformedByUserId",
                table: "NursingProcedures",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PerformedAt",
                table: "NursingProcedures",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<bool>(
                name: "IsBilled",
                table: "NursingProcedures",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "OrderedAt",
                table: "NursingProcedures",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "OrderedByDoctorId",
                table: "NursingProcedures",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "NursingProcedures",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "NursingProcedures",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Routine");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "NursingProcedures",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Completed");

            migrationBuilder.CreateTable(
                name: "RadiologyOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Modality = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProcedureName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BodyPart = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ClinicalIndication = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Routine"),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Ordered"),
                    OrderedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PerformedByTechnicianName = table.Column<string>(type: "text", nullable: true),
                    Findings = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Impression = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RadiologistSignature = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DoctorSignedOffAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DoctorSignOffNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsBilled = table.Column<bool>(type: "boolean", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadiologyOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RadiologyOrders_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RadiologyOrders_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RadiologyOrders_Users_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NursingProcedures_OrderedByDoctorId",
                table: "NursingProcedures",
                column: "OrderedByDoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_RadiologyOrders_AppointmentId",
                table: "RadiologyOrders",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RadiologyOrders_DoctorId",
                table: "RadiologyOrders",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_RadiologyOrders_Modality",
                table: "RadiologyOrders",
                column: "Modality");

            migrationBuilder.CreateIndex(
                name: "IX_RadiologyOrders_OrderedAt",
                table: "RadiologyOrders",
                column: "OrderedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RadiologyOrders_OrderNumber",
                table: "RadiologyOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RadiologyOrders_PatientId",
                table: "RadiologyOrders",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_RadiologyOrders_Status",
                table: "RadiologyOrders",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_NursingProcedures_Users_OrderedByDoctorId",
                table: "NursingProcedures",
                column: "OrderedByDoctorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NursingProcedures_Users_OrderedByDoctorId",
                table: "NursingProcedures");

            migrationBuilder.DropTable(
                name: "RadiologyOrders");

            migrationBuilder.DropIndex(
                name: "IX_NursingProcedures_OrderedByDoctorId",
                table: "NursingProcedures");

            migrationBuilder.DropColumn(
                name: "IsBilled",
                table: "NursingProcedures");

            migrationBuilder.DropColumn(
                name: "OrderedAt",
                table: "NursingProcedures");

            migrationBuilder.DropColumn(
                name: "OrderedByDoctorId",
                table: "NursingProcedures");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "NursingProcedures");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "NursingProcedures");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "NursingProcedures");

            migrationBuilder.AlterColumn<Guid>(
                name: "PerformedByUserId",
                table: "NursingProcedures",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "PerformedAt",
                table: "NursingProcedures",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
