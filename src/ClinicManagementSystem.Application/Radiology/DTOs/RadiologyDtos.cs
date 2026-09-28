using System;

namespace ClinicManagementSystem.Application.Radiology.DTOs;

public class RadiologyOrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string AccessionNumber { get; set; } = string.Empty;
    public string? RoomSuite { get; set; }
    public string? EquipmentModel { get; set; }
    public DateTime? ScheduledAt { get; set; }

    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientMedicalRecordNumber { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }

    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;

    public Guid? AppointmentId { get; set; }
    public string Modality { get; set; } = string.Empty;
    public string ProcedureName { get; set; } = string.Empty;
    public string BodyPart { get; set; } = string.Empty;
    public string ClinicalIndication { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime OrderedAt { get; set; }

    public DateTime? PerformedAt { get; set; }
    public string? PerformedByTechnicianName { get; set; }
    public string? Findings { get; set; }
    public string? Impression { get; set; }
    public string? ImageUrl { get; set; }
    public string? SecondaryAttachmentUrls { get; set; }
    public string? RadiologistSignature { get; set; }
    public DateTime? ReportedAt { get; set; }

    // Critical Alert
    public bool IsCritical { get; set; }
    public string? CriticalAlertNotes { get; set; }
    public bool CriticalAlertAcknowledgedByDoctor { get; set; }
    public DateTime? CriticalAlertAcknowledgedAt { get; set; }

    // Radiation & Contrast
    public decimal? RadiationDoseDlp { get; set; }
    public decimal? RadiationDoseDap { get; set; }
    public string? ExposureParameters { get; set; }
    public bool ContrastUsed { get; set; }
    public string? ContrastAgentName { get; set; }
    public decimal? ContrastVolumeMl { get; set; }
    public string? ContrastLotNumber { get; set; }
    public string? ContrastRoute { get; set; }

    public DateTime? DoctorSignedOffAt { get; set; }
    public string? DoctorSignOffNotes { get; set; }

    public decimal Price { get; set; }
    public bool IsBilled { get; set; }
}

public class CreateRadiologyOrderDto
{
    public Guid PatientId { get; set; }
    public Guid? AppointmentId { get; set; }
    public string Modality { get; set; } = "X-Ray";
    public string ProcedureName { get; set; } = string.Empty;
    public string BodyPart { get; set; } = string.Empty;
    public string ClinicalIndication { get; set; } = string.Empty;
    public string Priority { get; set; } = "Routine";
    public decimal Price { get; set; } = 0m;
    public string? RoomSuite { get; set; }
}

public class ScheduleRadiologyOrderDto
{
    public string RoomSuite { get; set; } = string.Empty;
    public string? EquipmentModel { get; set; }
    public DateTime? ScheduledAt { get; set; }
}

public class ReportRadiologyOrderDto
{
    public string Findings { get; set; } = string.Empty;
    public string Impression { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? SecondaryAttachmentUrls { get; set; }
    public string? RadiologistSignature { get; set; }

    public bool IsCritical { get; set; } = false;
    public string? CriticalAlertNotes { get; set; }

    public decimal? RadiationDoseDlp { get; set; }
    public decimal? RadiationDoseDap { get; set; }
    public string? ExposureParameters { get; set; }

    public bool ContrastUsed { get; set; } = false;
    public string? ContrastAgentName { get; set; }
    public decimal? ContrastVolumeMl { get; set; }
    public string? ContrastLotNumber { get; set; }
    public string? ContrastRoute { get; set; }
}

public class DoctorSignOffRadiologyDto
{
    public string? Notes { get; set; }
}

public class AcknowledgeCriticalAlertDto
{
    public string? AcknowledgmentNotes { get; set; }
}

public class RadiologyMetricsDto
{
    public int TotalOrders { get; set; }
    public int OrderedCount { get; set; }
    public int ScheduledCount { get; set; }
    public int InProgressCount { get; set; }
    public int CompletedCount { get; set; }
    public int PendingDoctorReviewCount { get; set; }
    public int StatUrgentCount { get; set; }
    public int CriticalAlertsCount { get; set; }
}

public class RadiologySafetyLogDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string AccessionNumber { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string Modality { get; set; } = string.Empty;
    public string ProcedureName { get; set; } = string.Empty;
    public DateTime? PerformedAt { get; set; }
    public string? PerformedByTechnicianName { get; set; }
    public decimal? RadiationDoseDlp { get; set; }
    public decimal? RadiationDoseDap { get; set; }
    public string? ExposureParameters { get; set; }
    public bool ContrastUsed { get; set; }
    public string? ContrastAgentName { get; set; }
    public decimal? ContrastVolumeMl { get; set; }
    public string? ContrastLotNumber { get; set; }
    public string? ContrastRoute { get; set; }
}

public class CreateRadiologySafetyEntryDto
{
    public string? AccessionNumber { get; set; }
    public string? PatientName { get; set; }
    public Guid? PatientId { get; set; }
    public string Modality { get; set; } = "CT Scan";
    public string ProcedureName { get; set; } = string.Empty;
    public string? PerformedByTechnicianName { get; set; }
    public decimal? RadiationDoseDlp { get; set; }
    public decimal? RadiationDoseDap { get; set; }
    public string? ExposureParameters { get; set; }
    public bool ContrastUsed { get; set; } = false;
    public string? ContrastAgentName { get; set; }
    public decimal? ContrastVolumeMl { get; set; }
    public string? ContrastLotNumber { get; set; }
    public string? ContrastRoute { get; set; }
}

public class UpdateRadiologyStatusDto
{
    public string Status { get; set; } = "InProgress";
    public string? Notes { get; set; }
}

