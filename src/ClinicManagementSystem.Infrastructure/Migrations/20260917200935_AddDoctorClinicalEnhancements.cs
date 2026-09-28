using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorClinicalEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VitalSigns_AppointmentId",
                table: "VitalSigns");

            migrationBuilder.AddColumn<string>(
                name: "Allergies",
                table: "Patients",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Findings",
                table: "Consultations",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_VitalSigns_AppointmentId",
                table: "VitalSigns",
                column: "AppointmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VitalSigns_AppointmentId",
                table: "VitalSigns");

            migrationBuilder.DropColumn(
                name: "Allergies",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "Findings",
                table: "Consultations");

            migrationBuilder.CreateIndex(
                name: "IX_VitalSigns_AppointmentId",
                table: "VitalSigns",
                column: "AppointmentId",
                unique: true);
        }
    }
}
