using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRadiologySuiteAdvancedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccessionNumber",
                table: "RadiologyOrders",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContrastAgentName",
                table: "RadiologyOrders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContrastLotNumber",
                table: "RadiologyOrders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContrastRoute",
                table: "RadiologyOrders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ContrastUsed",
                table: "RadiologyOrders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ContrastVolumeMl",
                table: "RadiologyOrders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CriticalAlertAcknowledgedAt",
                table: "RadiologyOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CriticalAlertAcknowledgedByDoctor",
                table: "RadiologyOrders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CriticalAlertNotes",
                table: "RadiologyOrders",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EquipmentModel",
                table: "RadiologyOrders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExposureParameters",
                table: "RadiologyOrders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCritical",
                table: "RadiologyOrders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "RadiationDoseDap",
                table: "RadiologyOrders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RadiationDoseDlp",
                table: "RadiologyOrders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoomSuite",
                table: "RadiologyOrders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledAt",
                table: "RadiologyOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryAttachmentUrls",
                table: "RadiologyOrders",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RadiologyOrders_AccessionNumber",
                table: "RadiologyOrders",
                column: "AccessionNumber");

            migrationBuilder.CreateIndex(
                name: "IX_RadiologyOrders_IsCritical",
                table: "RadiologyOrders",
                column: "IsCritical");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RadiologyOrders_AccessionNumber",
                table: "RadiologyOrders");

            migrationBuilder.DropIndex(
                name: "IX_RadiologyOrders_IsCritical",
                table: "RadiologyOrders");

            migrationBuilder.DropColumn(
                name: "AccessionNumber",
                table: "RadiologyOrders");

            migrationBuilder.DropColumn(
                name: "ContrastAgentName",
                table: "RadiologyOrders");

            migrationBuilder.DropColumn(
                name: "ContrastLotNumber",
                table: "RadiologyOrders");

            migrationBuilder.DropColumn(
                name: "ContrastRoute",
                table: "RadiologyOrders");

            migrationBuilder.DropColumn(
                name: "ContrastUsed",
                table: "RadiologyOrders");

            migrationBuilder.DropColumn(
                name: "ContrastVolumeMl",
                table: "RadiologyOrders");

            migrationBuilder.DropColumn(
                name: "CriticalAlertAcknowledgedAt",
                table: "RadiologyOrders");

            migrationBuilder.DropColumn(
                name: "CriticalAlertAcknowledgedByDoctor",
                table: "RadiologyOrders");

            migrationBuilder.DropColumn(
                name: "CriticalAlertNotes",
                table: "RadiologyOrders");

            migrationBuilder.DropColumn(
                name: "EquipmentModel",
                table: "RadiologyOrders");

            migrationBuilder.DropColumn(
                name: "ExposureParameters",
                table: "RadiologyOrders");

            migrationBuilder.DropColumn(
                name: "IsCritical",
                table: "RadiologyOrders");

            migrationBuilder.DropColumn(
                name: "RadiationDoseDap",
                table: "RadiologyOrders");

            migrationBuilder.DropColumn(
                name: "RadiationDoseDlp",
                table: "RadiologyOrders");

            migrationBuilder.DropColumn(
                name: "RoomSuite",
                table: "RadiologyOrders");

            migrationBuilder.DropColumn(
                name: "ScheduledAt",
                table: "RadiologyOrders");

            migrationBuilder.DropColumn(
                name: "SecondaryAttachmentUrls",
                table: "RadiologyOrders");
        }
    }
}
