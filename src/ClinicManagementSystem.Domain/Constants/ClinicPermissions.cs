namespace ClinicManagementSystem.Domain.Constants;

public static class ClinicPermissions
{
    // Patients & Clinical Demographics
    public const string PatientsView = "Patients.View";
    public const string PatientsCreate = "Patients.Create";
    public const string PatientsEdit = "Patients.Edit";
    public const string PatientsExport = "Patients.Export";

    // Consultations & EMR
    public const string ConsultationsView = "Consultations.View";
    public const string ConsultationsCreate = "Consultations.Create";
    public const string ConsultationsEdit = "Consultations.Edit";
    public const string ConsultationsSign = "Consultations.Sign";
    public const string EmrView = "Emr.View";
    public const string EmrBreakGlass = "Emr.BreakGlass";

    // Appointments & Queue
    public const string AppointmentsView = "Appointments.View";
    public const string AppointmentsBook = "Appointments.Book";
    public const string AppointmentsCancel = "Appointments.Cancel";
    public const string AppointmentsCheckIn = "Appointments.CheckIn";
    public const string AppointmentsManage = "Appointments.Manage";

    // Prescriptions & Pharmacy
    public const string PrescriptionsView = "Prescriptions.View";
    public const string PrescriptionsCreate = "Prescriptions.Create";
    public const string PrescriptionsIssue = "Prescriptions.Issue";
    public const string PrescriptionsDispense = "Prescriptions.Dispense";
    public const string MedicinesView = "Medicines.View";
    public const string MedicinesManage = "Medicines.Manage";
    public const string PharmacyManageInventory = "Pharmacy.ManageInventory";
    public const string PharmacyRequisitions = "Pharmacy.Requisitions";
    public const string PharmacyAudit = "Pharmacy.Audit";

    // Laboratory & Diagnostics
    public const string LabOrdersView = "LabOrders.View";
    public const string LabOrdersCreate = "LabOrders.Create";
    public const string LabOrdersProcess = "LabOrders.Process";
    public const string LabOrdersVerify = "LabOrders.Verify";
    public const string LabOrder = "Lab.Order";
    public const string LabAnalyze = "Lab.Analyze";
    public const string LabVerifyResults = "Lab.VerifyResults";
    public const string LabInventory = "Lab.Inventory";
    public const string LabEquipment = "Lab.Equipment";
    public const string LabRequisitions = "Lab.Requisitions";

    // Radiology & PACS
    public const string RadiologyView = "Radiology.View";
    public const string RadiologyOrder = "Radiology.Order";
    public const string RadiologyReport = "Radiology.Report";
    public const string RadiologySignOff = "Radiology.SignOff";
    public const string RadiologySafetyDose = "Radiology.SafetyDose";
    public const string RadiologySuites = "Radiology.Suites";

    // Billing & Finance
    public const string BillingView = "Billing.View";
    public const string BillingCreateInvoice = "Billing.CreateInvoice";
    public const string BillingProcessPayment = "Billing.ProcessPayment";
    public const string BillingReports = "Billing.Reports";
    public const string BillingApplyDiscount = "Billing.ApplyDiscount";
    public const string BillingCollectPayment = "Billing.CollectPayment";
    public const string BillingClaims = "Billing.Claims";
    public const string BillingAdjustments = "Billing.Adjustments";
    public const string BillingExpenses = "Billing.Expenses";
    public const string BillingPettyCash = "Billing.PettyCash";
    public const string BillingPayroll = "Billing.Payroll";
    public const string BillingReconciliation = "Billing.Reconciliation";

    // Governance & Duty
    public const string StaffDutyClockInOut = "StaffDuty.ClockInOut";
    public const string ClinicalGovernanceBreakGlass = "ClinicalGovernance.BreakGlass";
    public const string ClinicalGovernanceDelegateAuthority = "ClinicalGovernance.DelegateAuthority";
    public const string GovernanceBreakGlass = "Governance.BreakGlass";
    public const string GovernanceDelegateAuthority = "Governance.DelegateAuthority";

    // Administration, Audit & Security
    public const string UserManagementView = "UserManagement.View";
    public const string UserManagementManageRoles = "UserManagement.ManageRoles";
    public const string UserManagementAuditLogs = "UserManagement.AuditLogs";
    public const string UsersManage = "Users.Manage";
    public const string AuditView = "Audit.View";

    public static readonly IReadOnlyList<string> All = new[]
    {
        // 36 Canonical PBAC Matrix Keys
        PatientsView, PatientsCreate, PatientsEdit, PatientsExport,
        ConsultationsView, ConsultationsCreate, ConsultationsEdit, ConsultationsSign,
        AppointmentsView, AppointmentsBook, AppointmentsCancel, AppointmentsCheckIn,
        PrescriptionsView, PrescriptionsCreate, PrescriptionsDispense, PharmacyManageInventory,
        LabOrdersView, LabOrdersCreate, LabOrdersProcess, LabOrdersVerify,
        RadiologyView, RadiologyOrder, RadiologyReport, RadiologySignOff, RadiologySafetyDose, RadiologySuites,
        BillingView, BillingCreateInvoice, BillingProcessPayment, BillingReports,
        StaffDutyClockInOut, ClinicalGovernanceBreakGlass, ClinicalGovernanceDelegateAuthority,
        UserManagementView, UserManagementManageRoles, UserManagementAuditLogs,

        // Domain & Functional Aliases
        AppointmentsManage, EmrView, EmrBreakGlass,
        PrescriptionsIssue, MedicinesView, MedicinesManage, PharmacyRequisitions, PharmacyAudit,
        LabOrder, LabAnalyze, LabVerifyResults, LabInventory, LabEquipment, LabRequisitions,
        BillingApplyDiscount, BillingCollectPayment, BillingClaims, BillingAdjustments, BillingExpenses, BillingPettyCash, BillingPayroll, BillingReconciliation,
        GovernanceBreakGlass, GovernanceDelegateAuthority, UsersManage, AuditView
    };
}
