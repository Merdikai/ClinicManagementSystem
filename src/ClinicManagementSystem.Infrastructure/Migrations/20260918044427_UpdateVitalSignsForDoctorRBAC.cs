using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVitalSignsForDoctorRBAC : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VitalSigns_Users_RecordedByNurseId",
                table: "VitalSigns");

            migrationBuilder.RenameColumn(
                name: "RecordedByNurseId",
                table: "VitalSigns",
                newName: "RecordedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_VitalSigns_RecordedByNurseId",
                table: "VitalSigns",
                newName: "IX_VitalSigns_RecordedByUserId");

            migrationBuilder.AddColumn<Guid>(
                name: "ConsultationId",
                table: "VitalSigns",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VitalSigns_ConsultationId",
                table: "VitalSigns",
                column: "ConsultationId");

            migrationBuilder.AddForeignKey(
                name: "FK_VitalSigns_Consultations_ConsultationId",
                table: "VitalSigns",
                column: "ConsultationId",
                principalTable: "Consultations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VitalSigns_Users_RecordedByUserId",
                table: "VitalSigns",
                column: "RecordedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VitalSigns_Consultations_ConsultationId",
                table: "VitalSigns");

            migrationBuilder.DropForeignKey(
                name: "FK_VitalSigns_Users_RecordedByUserId",
                table: "VitalSigns");

            migrationBuilder.DropIndex(
                name: "IX_VitalSigns_ConsultationId",
                table: "VitalSigns");

            migrationBuilder.DropColumn(
                name: "ConsultationId",
                table: "VitalSigns");

            migrationBuilder.RenameColumn(
                name: "RecordedByUserId",
                table: "VitalSigns",
                newName: "RecordedByNurseId");

            migrationBuilder.RenameIndex(
                name: "IX_VitalSigns_RecordedByUserId",
                table: "VitalSigns",
                newName: "IX_VitalSigns_RecordedByNurseId");

            migrationBuilder.AddForeignKey(
                name: "FK_VitalSigns_Users_RecordedByNurseId",
                table: "VitalSigns",
                column: "RecordedByNurseId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
