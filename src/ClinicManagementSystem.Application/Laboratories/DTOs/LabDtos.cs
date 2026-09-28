using System;
using System.Collections.Generic;
using ClinicManagementSystem.Domain.Enums;

namespace ClinicManagementSystem.Application.Laboratories.DTOs;

public class LabTestTemplateDto
{
    public Guid Id { get; set; }
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SampleType { get; set; } = string.Empty;
    public int TurnaroundTimeHours { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public string ParametersJson { get; set; } = "[]";
}

public class CreateLabTestTemplateDto
{
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string? Description { get; set; }
    public string SampleType { get; set; } = "Blood";
    public int TurnaroundTimeHours { get; set; } = 24;
    public decimal Price { get; set; }
    public string ParametersJson { get; set; } = "[]";
}

public class UpdateLabTestTemplateDto
{
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string? Description { get; set; }
    public string SampleType { get; set; } = "Blood";
    public int TurnaroundTimeHours { get; set; } = 24;
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public string ParametersJson { get; set; } = "[]";
}

public class LabOrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientMedicalRecordNumber { get; set; } = string.Empty;
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public Guid? AppointmentId { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = "Ordered";
    public string Priority { get; set; } = "Routine";
    public string? ClinicalNotes { get; set; }
    public decimal TotalCost { get; set; }
    public bool IsBilled { get; set; }
    public DateTime? SampleCollectedAt { get; set; }
    public string? CollectedByTechnicianName { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Specimen Barcode & Accession Details
    public string? SpecimenBarcode { get; set; }
    public string? SpecimenType { get; set; }
    public string? SampleCondition { get; set; }
    public bool IsRejected { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectedByTechnicianName { get; set; }
    public bool RequiresRecollection { get; set; }
    public string? TechnicianNotes { get; set; }

    public List<LabOrderItemDto> Items { get; set; } = new();
}

public class LabOrderItemDto
{
    public Guid Id { get; set; }
    public Guid LabOrderId { get; set; }
    public Guid LabTestTemplateId { get; set; }
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SampleType { get; set; } = string.Empty;
    public string Status { get; set; } = "Ordered";
    public decimal Price { get; set; }
    public string? SpecimenBarcode { get; set; }
    public bool IsRejected { get; set; }
    public string? RejectionReason { get; set; }
    public LabResultDto? Result { get; set; }
}

public class LabResultDto
{
    public Guid Id { get; set; }
    public Guid LabOrderItemId { get; set; }
    public Guid? PerformedByTechnicianId { get; set; }
    public string? PerformedByTechnicianName { get; set; }
    public Guid? VerifiedByDoctorId { get; set; }
    public string? VerifiedByDoctorName { get; set; }
    public DateTime ResultDate { get; set; }
    public string ParameterResultsJson { get; set; } = "[]";
    public string? Remarks { get; set; }
    public bool HasAbnormalFlag { get; set; }
    public string? AttachmentUrl { get; set; }
}

public class CreateLabOrderRequest
{
    public Guid PatientId { get; set; }
    public Guid? DoctorId { get; set; }
    public Guid? AppointmentId { get; set; }
    public string Priority { get; set; } = "Routine";
    public string? ClinicalNotes { get; set; }
    public List<Guid> TestTemplateIds { get; set; } = new();
}

public class RecordLabResultRequest
{
    public Guid LabOrderItemId { get; set; }
    public string ParameterResultsJson { get; set; } = "[]";
    public string? Remarks { get; set; }
    public bool HasAbnormalFlag { get; set; }
    public string? AttachmentUrl { get; set; }
}

public class SpecimenIntakeRequest
{
    public Guid LabOrderId { get; set; }
    public string SpecimenType { get; set; } = "Whole Blood";
    public string TubeType { get; set; } = "EDTA Lavender Top";
    public string SampleCondition { get; set; } = "Viable";
    public decimal? CollectionVolumeMl { get; set; }
    public string? TechnicianNotes { get; set; }
}

public class SpecimenRejectRequest
{
    public Guid LabOrderId { get; set; }
    public Guid? OrderItemId { get; set; }
    public string RejectionReason { get; set; } = "Severe Hemolysis";
    public bool RequiresRecollection { get; set; } = true;
    public string? Notes { get; set; }
}

public class LabReagentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CatalogCode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string LotNumber { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int MinimumThreshold { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public string StorageCondition { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public decimal UnitCost { get; set; }
    public DateTime? LastRestockedAt { get; set; }
    public string? Notes { get; set; }
}

public class CreateLabReagentRequest
{
    public string Name { get; set; } = string.Empty;
    public string CatalogCode { get; set; } = string.Empty;
    public string Category { get; set; } = "Hematology";
    public string LotNumber { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int MinimumThreshold { get; set; } = 10;
    public string UnitOfMeasure { get; set; } = "Tests";
    public DateTime ExpiryDate { get; set; }
    public string StorageCondition { get; set; } = "2-8°C Refrigerated";
    public string? Manufacturer { get; set; }
    public decimal UnitCost { get; set; }
    public string? Notes { get; set; }
}

public class UpdateReagentStockRequest
{
    public int AdjustmentQuantity { get; set; }
    public string Reason { get; set; } = "Restock replenishment";
}

public class LabStockRequisitionDto
{
    public Guid Id { get; set; }
    public string RequisitionNumber { get; set; } = string.Empty;
    public Guid? LabReagentId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int QuantityRequested { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal EstimatedUnitCost { get; set; }
    public decimal TotalEstimatedCost { get; set; }
    public string Urgency { get; set; } = "Routine";
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public Guid RequestedByUserId { get; set; }
    public string RequestedByUserName { get; set; } = string.Empty;
    public Guid? ApprovedByUserId { get; set; }
    public string? ApprovedByUserName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateLabRequisitionRequest
{
    public Guid? LabReagentId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string Category { get; set; } = "Hematology";
    public int QuantityRequested { get; set; }
    public string UnitOfMeasure { get; set; } = "Kits";
    public decimal EstimatedUnitCost { get; set; }
    public string Urgency { get; set; } = "Routine";
    public string Reason { get; set; } = string.Empty;
}

public class ReviewLabRequisitionRequest
{
    public string Status { get; set; } = "Approved"; // Approved, Rejected, Fulfilled
    public string? ReviewNotes { get; set; }
}

public class LabEquipmentLogDto
{
    public Guid Id { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public string EquipmentModel { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string LogType { get; set; } = string.Empty;
    public string? ControlLotNumber { get; set; }
    public decimal? StandardDeviationScore { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Findings { get; set; } = string.Empty;
    public string? CorrectiveAction { get; set; }
    public Guid PerformedByUserId { get; set; }
    public string PerformedByUserName { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; }
    public DateTime? NextMaintenanceDueDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateLabEquipmentLogRequest
{
    public string EquipmentName { get; set; } = string.Empty;
    public string EquipmentModel { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Location { get; set; } = "Main Laboratory";
    public string LogType { get; set; } = "DailyCalibration";
    public string? ControlLotNumber { get; set; }
    public decimal? StandardDeviationScore { get; set; }
    public string Status { get; set; } = "Passed";
    public string Findings { get; set; } = string.Empty;
    public string? CorrectiveAction { get; set; }
    public DateTime? NextMaintenanceDueDate { get; set; }
}

public class LabMetricsDto
{
    public int PendingOrders { get; set; }
    public int SamplesCollected { get; set; }
    public int InProgress { get; set; }
    public int CompletedToday { get; set; }
    public int TotalTests { get; set; }
    public int CriticalValueAlerts { get; set; }
    public int RejectedSamples { get; set; }
    public int LowStockReagents { get; set; }
    public int CalibrationsDue { get; set; }
}
