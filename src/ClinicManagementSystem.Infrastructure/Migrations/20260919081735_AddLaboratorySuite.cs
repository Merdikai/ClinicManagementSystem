using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLaboratorySuite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CollectedByTechnicianName",
                table: "LabOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRejected",
                table: "LabOrders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "LabOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectedByTechnicianName",
                table: "LabOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "LabOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresRecollection",
                table: "LabOrders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SampleCondition",
                table: "LabOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecimenBarcode",
                table: "LabOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecimenType",
                table: "LabOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicianNotes",
                table: "LabOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRejected",
                table: "LabOrderItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "LabOrderItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecimenBarcode",
                table: "LabOrderItems",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LabEquipmentLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EquipmentModel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LogType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ControlLotNumber = table.Column<string>(type: "text", nullable: true),
                    StandardDeviationScore = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Findings = table.Column<string>(type: "text", nullable: false),
                    CorrectiveAction = table.Column<string>(type: "text", nullable: true),
                    PerformedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PerformedByUserName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextMaintenanceDueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabEquipmentLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LabReagents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CatalogCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CurrentStock = table.Column<int>(type: "integer", nullable: false),
                    MinimumThreshold = table.Column<int>(type: "integer", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StorageCondition = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LastRestockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabReagents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LabStockRequisitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequisitionNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LabReagentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    QuantityRequested = table.Column<int>(type: "integer", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EstimatedUnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalEstimatedCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Urgency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserName = table.Column<string>(type: "text", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedByUserName = table.Column<string>(type: "text", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabStockRequisitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabStockRequisitions_LabReagents_LabReagentId",
                        column: x => x.LabReagentId,
                        principalTable: "LabReagents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LabStockRequisitions_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabStockRequisitions_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LabStockRequisitions_ApprovedByUserId",
                table: "LabStockRequisitions",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LabStockRequisitions_LabReagentId",
                table: "LabStockRequisitions",
                column: "LabReagentId");

            migrationBuilder.CreateIndex(
                name: "IX_LabStockRequisitions_RequestedByUserId",
                table: "LabStockRequisitions",
                column: "RequestedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LabEquipmentLogs");

            migrationBuilder.DropTable(
                name: "LabStockRequisitions");

            migrationBuilder.DropTable(
                name: "LabReagents");

            migrationBuilder.DropColumn(
                name: "CollectedByTechnicianName",
                table: "LabOrders");

            migrationBuilder.DropColumn(
                name: "IsRejected",
                table: "LabOrders");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "LabOrders");

            migrationBuilder.DropColumn(
                name: "RejectedByTechnicianName",
                table: "LabOrders");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "LabOrders");

            migrationBuilder.DropColumn(
                name: "RequiresRecollection",
                table: "LabOrders");

            migrationBuilder.DropColumn(
                name: "SampleCondition",
                table: "LabOrders");

            migrationBuilder.DropColumn(
                name: "SpecimenBarcode",
                table: "LabOrders");

            migrationBuilder.DropColumn(
                name: "SpecimenType",
                table: "LabOrders");

            migrationBuilder.DropColumn(
                name: "TechnicianNotes",
                table: "LabOrders");

            migrationBuilder.DropColumn(
                name: "IsRejected",
                table: "LabOrderItems");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "LabOrderItems");

            migrationBuilder.DropColumn(
                name: "SpecimenBarcode",
                table: "LabOrderItems");
        }
    }
}
