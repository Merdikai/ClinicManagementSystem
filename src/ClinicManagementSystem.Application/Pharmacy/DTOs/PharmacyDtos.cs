using System;
using System.Collections.Generic;

namespace ClinicManagementSystem.Application.Pharmacy.DTOs;

public class PharmacyMetricsDto
{
    public int PendingPrescriptionsCount { get; set; }
    public int DispensedTodayCount { get; set; }
    public int LowStockMedicinesCount { get; set; }
    public int NearExpiryMedicinesCount { get; set; }
    public int ControlledSubstancesCount { get; set; }
    public decimal TodayDispensaryRevenue { get; set; }
}

public class PharmacyStockRequisitionDto
{
    public Guid Id { get; set; }
    public string RequisitionNumber { get; set; } = string.Empty;
    public Guid MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string MedicineCode { get; set; } = string.Empty;
    public int QuantityRequested { get; set; }
    public int QuantityApproved { get; set; }
    public string Type { get; set; } = "PurchaseOrder";
    public string Department { get; set; } = "Main Pharmacy";
    public string? SupplierName { get; set; }
    public string Status { get; set; } = "Pending";
    public string RequestedByUserName { get; set; } = string.Empty;
    public string? ApprovedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal EstimatedUnitCost { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
}

public record CreateStockRequisitionDto(
    Guid MedicineId,
    int QuantityRequested,
    string Type,
    string Department,
    string? SupplierName = null,
    string? Notes = null
);

public record ReviewStockRequisitionDto(
    string Status,
    int? QuantityApproved = null,
    string? ReviewNotes = null
);

public class ControlledSubstanceLogDto
{
    public Guid Id { get; set; }
    public string LogNumber { get; set; } = string.Empty;
    public Guid MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string? Strength { get; set; }
    public string? DosageForm { get; set; }
    public Guid? PrescriptionId { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientMrn { get; set; } = string.Empty;
    public string PrescribingDoctorName { get; set; } = string.Empty;
    public string PharmacistName { get; set; } = string.Empty;
    public string? WitnessPharmacistName { get; set; }
    public int QuantityDispensed { get; set; }
    public int RemainingBalance { get; set; }
    public string MedicalJustification { get; set; } = string.Empty;
    public DateTime DispensedAt { get; set; }
}

