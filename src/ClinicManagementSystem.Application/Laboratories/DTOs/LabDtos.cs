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
    public DateTime? CompletedAt { get; set; }
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
    public LabResultDto? Result { get; set; }
}

public class LabResultDto
{
    public Guid Id { get; set; }
    public Guid LabOrderItemId { get; set; }
    public Guid? PerformedByTechnicianId { get; set; }
    public string? PerformedByTechnicianName { get; set; }
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

public class LabMetricsDto
{
    public int PendingOrders { get; set; }
    public int SamplesCollected { get; set; }
    public int InProgress { get; set; }
    public int CompletedToday { get; set; }
    public int TotalTests { get; set; }
}
