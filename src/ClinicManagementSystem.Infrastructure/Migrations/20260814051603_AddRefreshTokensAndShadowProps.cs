using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokensAndShadowProps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "VitalSigns",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "VitalSigns",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "UserRoles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "UserRoles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "Roles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "Roles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "Prescriptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "Prescriptions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "PrescriptionItems",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "PrescriptionItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "Payments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "Payments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "Patients",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "Patients",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "Medicines",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "Medicines",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "MedicalRecords",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "MedicalRecords",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "Invoices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "InvoiceItems",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "InvoiceItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "DoctorSchedules",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "DoctorSchedules",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "Consultations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "Consultations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "AuditLogs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "AuditLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "Appointments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "Appointments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "VitalSigns");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "VitalSigns");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "PrescriptionItems");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "PrescriptionItems");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "DoctorSchedules");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "DoctorSchedules");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "Appointments");
        }
    }
}
