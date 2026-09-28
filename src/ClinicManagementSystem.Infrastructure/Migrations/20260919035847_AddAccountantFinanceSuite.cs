using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountantFinanceSuite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingNotes",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "Invoices",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AuditNotes",
                table: "DailyCashReconciliations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuditStatus",
                table: "DailyCashReconciliations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "AuditedAt",
                table: "DailyCashReconciliations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AuditedByUserId",
                table: "DailyCashReconciliations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuditedByUserName",
                table: "DailyCashReconciliations",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BillingAdjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdjustmentNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingAdjustments_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpenseNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    VendorName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    InvoiceReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaidDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PharmacyStockRequisitionId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseTransactions_PharmacyStockRequisitions_PharmacyStock~",
                        column: x => x.PharmacyStockRequisitionId,
                        principalTable: "PharmacyStockRequisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InsuranceClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    InsuranceProvider = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PolicyNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    GroupNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ClaimAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CoveredAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PatientCopay = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RemittanceReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DenialReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SettledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsuranceClaims_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InsuranceClaims_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StaffUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PeriodMonth = table.Column<int>(type: "integer", nullable: false),
                    PeriodYear = table.Column<int>(type: "integer", nullable: false),
                    BaseSalary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ConsultationsCount = table.Column<int>(type: "integer", nullable: false),
                    DoctorCommissionShare = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OvertimeHours = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OvertimePay = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxDeductions = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NetSalaryPayable = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisbursedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollRecords_Users_StaffUserId",
                        column: x => x.StaffUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PettyCashVouchers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VoucherNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Payee = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DisbursedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisbursedByUserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RemainingBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceiptRef = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PettyCashVouchers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillingAdjustments_AdjustmentNumber",
                table: "BillingAdjustments",
                column: "AdjustmentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingAdjustments_InvoiceId",
                table: "BillingAdjustments",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseTransactions_ExpenseNumber",
                table: "ExpenseTransactions",
                column: "ExpenseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseTransactions_PharmacyStockRequisitionId",
                table: "ExpenseTransactions",
                column: "PharmacyStockRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_ClaimNumber",
                table: "InsuranceClaims",
                column: "ClaimNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_InvoiceId",
                table: "InsuranceClaims",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_PatientId",
                table: "InsuranceClaims",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRecords_PayrollNumber",
                table: "PayrollRecords",
                column: "PayrollNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRecords_StaffUserId",
                table: "PayrollRecords",
                column: "StaffUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashVouchers_VoucherNumber",
                table: "PettyCashVouchers",
                column: "VoucherNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillingAdjustments");

            migrationBuilder.DropTable(
                name: "ExpenseTransactions");

            migrationBuilder.DropTable(
                name: "InsuranceClaims");

            migrationBuilder.DropTable(
                name: "PayrollRecords");

            migrationBuilder.DropTable(
                name: "PettyCashVouchers");

            migrationBuilder.DropColumn(
                name: "BillingNotes",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "AuditNotes",
                table: "DailyCashReconciliations");

            migrationBuilder.DropColumn(
                name: "AuditStatus",
                table: "DailyCashReconciliations");

            migrationBuilder.DropColumn(
                name: "AuditedAt",
                table: "DailyCashReconciliations");

            migrationBuilder.DropColumn(
                name: "AuditedByUserId",
                table: "DailyCashReconciliations");

            migrationBuilder.DropColumn(
                name: "AuditedByUserName",
                table: "DailyCashReconciliations");
        }
    }
}
