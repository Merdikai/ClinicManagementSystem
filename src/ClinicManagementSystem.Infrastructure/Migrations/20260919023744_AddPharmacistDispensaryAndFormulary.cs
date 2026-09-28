using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPharmacistDispensaryAndFormulary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DispensedAt",
                table: "Prescriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DispensedByUserId",
                table: "Prescriptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InvoiceId",
                table: "Prescriptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Prescriptions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VerificationNotes",
                table: "Prescriptions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DispensedQuantity",
                table: "PrescriptionItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "PrescriptionItems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "BatchNumber",
                table: "Medicines",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<decimal>(
                name: "CostPrice",
                table: "Medicines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DosageForm",
                table: "Medicines",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GenericName",
                table: "Medicines",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Medicines",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsControlledSubstance",
                table: "Medicines",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Manufacturer",
                table: "Medicines",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ReorderLevel",
                table: "Medicines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StorageConditions",
                table: "Medicines",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Strength",
                table: "Medicines",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ControlledSubstanceLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LogNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MedicineId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrescriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityDispensed = table.Column<int>(type: "integer", nullable: false),
                    RemainingBalance = table.Column<int>(type: "integer", nullable: false),
                    PrescribingDoctorName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DispensingPharmacistUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    WitnessPharmacistName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    DispensedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MedicalJustification = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlledSubstanceLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ControlledSubstanceLogs_Medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalTable: "Medicines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ControlledSubstanceLogs_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ControlledSubstanceLogs_Prescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalTable: "Prescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ControlledSubstanceLogs_Users_DispensingPharmacistUserId",
                        column: x => x.DispensingPharmacistUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PharmacyStockRequisitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequisitionNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MedicineId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityRequested = table.Column<int>(type: "integer", nullable: false),
                    QuantityApproved = table.Column<int>(type: "integer", nullable: false),
                    EstimatedUnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SupplierName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PharmacyStockRequisitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PharmacyStockRequisitions_Medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalTable: "Medicines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PharmacyStockRequisitions_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PharmacyStockRequisitions_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_DispensedByUserId",
                table: "Prescriptions",
                column: "DispensedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_InvoiceId",
                table: "Prescriptions",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlledSubstanceLogs_DispensingPharmacistUserId",
                table: "ControlledSubstanceLogs",
                column: "DispensingPharmacistUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlledSubstanceLogs_LogNumber",
                table: "ControlledSubstanceLogs",
                column: "LogNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ControlledSubstanceLogs_MedicineId",
                table: "ControlledSubstanceLogs",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlledSubstanceLogs_PatientId",
                table: "ControlledSubstanceLogs",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlledSubstanceLogs_PrescriptionId",
                table: "ControlledSubstanceLogs",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyStockRequisitions_ApprovedByUserId",
                table: "PharmacyStockRequisitions",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyStockRequisitions_MedicineId",
                table: "PharmacyStockRequisitions",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyStockRequisitions_RequestedByUserId",
                table: "PharmacyStockRequisitions",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyStockRequisitions_RequisitionNumber",
                table: "PharmacyStockRequisitions",
                column: "RequisitionNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Prescriptions_Invoices_InvoiceId",
                table: "Prescriptions",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Prescriptions_Users_DispensedByUserId",
                table: "Prescriptions",
                column: "DispensedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Prescriptions_Invoices_InvoiceId",
                table: "Prescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Prescriptions_Users_DispensedByUserId",
                table: "Prescriptions");

            migrationBuilder.DropTable(
                name: "ControlledSubstanceLogs");

            migrationBuilder.DropTable(
                name: "PharmacyStockRequisitions");

            migrationBuilder.DropIndex(
                name: "IX_Prescriptions_DispensedByUserId",
                table: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_Prescriptions_InvoiceId",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "DispensedAt",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "DispensedByUserId",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "InvoiceId",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "VerificationNotes",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "DispensedQuantity",
                table: "PrescriptionItems");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "PrescriptionItems");

            migrationBuilder.DropColumn(
                name: "CostPrice",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "DosageForm",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "GenericName",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "IsControlledSubstance",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "Manufacturer",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "ReorderLevel",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "StorageConditions",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "Strength",
                table: "Medicines");

            migrationBuilder.AlterColumn<string>(
                name: "BatchNumber",
                table: "Medicines",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);
        }
    }
}
