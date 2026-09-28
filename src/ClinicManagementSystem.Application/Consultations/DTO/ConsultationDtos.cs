using System;
using System.Collections.Generic;
using ClinicManagementSystem.Application.Radiology.DTOs;

namespace ClinicManagementSystem.Application.DTOs;

public class CreateNursingProcedureOrderDto
{
    public string ProcedureType { get; set; } = string.Empty;
    public string BodySite { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Priority { get; set; } = "Routine";
    public decimal Price { get; set; } = 0m;
}

public class CreateConsultationDto
{
    public Guid AppointmentId { get; set; }
    public string Symptoms { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string Findings { get; set; } = string.Empty;
    public string ClinicalNotes { get; set; } = string.Empty;
    public string? PrescriptionNotes { get; set; }
    public List<CreatePrescriptionItemDto>? PrescriptionItems { get; set; }

    // Session vitals (optional)
    public decimal? SystolicBP { get; set; }
    public decimal? DiastolicBP { get; set; }
    public decimal? TemperatureC { get; set; }
    public decimal? HeartRateBpm { get; set; }
    public decimal? RespiratoryRate { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? HeightCm { get; set; }

    // Integrated Multi-Order Dispatch
    public List<Guid>? LabTestTemplateIds { get; set; }
    public string? LabPriority { get; set; }
    public string? LabClinicalNotes { get; set; }

    public List<CreateRadiologyOrderDto>? RadiologyOrders { get; set; }
    public List<CreateNursingProcedureOrderDto>? NursingProcedureOrders { get; set; }

    public CreateConsultationDto() { }

    public CreateConsultationDto(Guid appointmentId, string symptoms, string diagnosis, string clinicalNotes, string findings = "")
    {
        AppointmentId = appointmentId;
        Symptoms = symptoms;
        Diagnosis = diagnosis;
        Findings = findings;
        ClinicalNotes = clinicalNotes;
    }
}

public class ConsultationResponseDto
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Symptoms { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string Findings { get; set; } = string.Empty;
    public string ClinicalNotes { get; set; } = string.Empty;
    public DateTime ConsultedAt { get; set; }
    public PrescriptionResponseDto? Prescription { get; set; }

    public Guid? LabOrderId { get; set; }
    public List<Guid>? RadiologyOrderIds { get; set; }
    public List<Guid>? NursingProcedureIds { get; set; }
}
