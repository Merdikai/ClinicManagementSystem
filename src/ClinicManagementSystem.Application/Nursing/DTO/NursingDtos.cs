using System;

namespace ClinicManagementSystem.Application.DTOs;

public record CreateNursingProcedureDto(
    Guid PatientId,
    Guid? AppointmentId,
    string ProcedureType,
    string? BodySite,
    string? Notes,
    string? PatientTolerance = "Well Tolerated",
    string? SuppliesUsed = null,
    DateTime? PerformedAt = null
);

public record OrderNursingProcedureRequestDto(
    Guid PatientId,
    Guid? AppointmentId,
    string ProcedureType,
    string? BodySite,
    string? Notes,
    string Priority = "Routine",
    decimal Price = 30.00m
);

public record CompleteNursingProcedureRequestDto(
    string? PatientTolerance,
    string? SuppliesUsed,
    string? Notes
);

public class NursingProcedureResponseDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public Guid? AppointmentId { get; set; }
    public Guid? OrderedByDoctorId { get; set; }
    public string? OrderedByDoctorName { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public string? PerformedByUserName { get; set; }
    public string ProcedureType { get; set; } = string.Empty;
    public string BodySite { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string PatientTolerance { get; set; } = string.Empty;
    public string SuppliesUsed { get; set; } = string.Empty;
    public string Status { get; set; } = "Completed";
    public string Priority { get; set; } = "Routine";
    public DateTime OrderedAt { get; set; }
    public DateTime? PerformedAt { get; set; }
    public decimal Price { get; set; }
    public bool IsBilled { get; set; }
}

public record CreateShiftHandoverDto(
    string ShiftType,
    string Situation,
    string Background,
    string Assessment,
    string Recommendation,
    int ActivePatientCount = 0,
    int CriticalAlertsCount = 0,
    DateTime? HandoverTimestamp = null
);

public class ShiftHandoverResponseDto
{
    public Guid Id { get; set; }
    public Guid NurseUserId { get; set; }
    public string NurseUserName { get; set; } = string.Empty;
    public string ShiftType { get; set; } = string.Empty;
    public DateTime HandoverTimestamp { get; set; }
    public string Situation { get; set; } = string.Empty;
    public string Background { get; set; } = string.Empty;
    public string Assessment { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public int ActivePatientCount { get; set; }
    public int CriticalAlertsCount { get; set; }
}
