using System;

namespace ClinicManagementSystem.Domain.Entities;

public class RadiologyOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OrderNumber { get; set; } = string.Empty; // e.g. RAD-20260919-0001

    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid DoctorId { get; set; }
    public User Doctor { get; set; } = null!;

    public Guid? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public string Modality { get; set; } = "X-Ray"; // X-Ray, Ultrasound, CT Scan, MRI, ECG / Echo
    public string ProcedureName { get; set; } = string.Empty; // e.g. "Chest X-Ray PA & Lateral"
    public string BodyPart { get; set; } = string.Empty; // Chest, Abdomen, Pelvis, Head/Neck, Extremities, Spine
    public string ClinicalIndication { get; set; } = string.Empty;
    public string Priority { get; set; } = "Routine"; // Routine, Urgent, Stat
    public string Status { get; set; } = "Ordered"; // Ordered, Scheduled, InProgress, Completed, Cancelled
    public DateTime OrderedAt { get; set; } = DateTime.UtcNow;

    // RIS Workflow & Room Scheduling
    public string AccessionNumber { get; set; } = string.Empty; // Unique RIS identifier e.g. ACC-20260920-0042
    public string? RoomSuite { get; set; } // e.g. "Suite 1 - Digital X-Ray", "Suite 2 - Siemens SOMATOM CT"
    public string? EquipmentModel { get; set; } // e.g. "Siemens SOMATOM go.Top", "GE SIGNA Creator 1.5T"
    public DateTime? ScheduledAt { get; set; }

    // Technical execution and Radiologist reporting
    public DateTime? PerformedAt { get; set; }
    public string? PerformedByTechnicianName { get; set; }
    public string? Findings { get; set; } // Detailed technical radiological observations
    public string? Impression { get; set; } // Official diagnostic impression
    public string? ImageUrl { get; set; } // Primary PACS preview or DICOM scan URL
    public string? SecondaryAttachmentUrls { get; set; } // JSON array of scan slices or report PDFs
    public string? RadiologistSignature { get; set; }
    public DateTime? ReportedAt { get; set; }

    // Critical Findings Alerting (Emergency MD Notification)
    public bool IsCritical { get; set; } = false;
    public string? CriticalAlertNotes { get; set; } // e.g. "Immediate notification: Tension pneumothorax right hemithorax"
    public bool CriticalAlertAcknowledgedByDoctor { get; set; } = false;
    public DateTime? CriticalAlertAcknowledgedAt { get; set; }

    // Radiation Dosimetry & Safety Compliance
    public decimal? RadiationDoseDlp { get; set; } // Dose Length Product (mGy*cm) for CT
    public decimal? RadiationDoseDap { get; set; } // Dose Area Product (dGy*cm²) for X-Ray / Fluoroscopy
    public string? ExposureParameters { get; set; } // e.g. "120 kVp, 250 mAs, 0.6mm slice thickness"

    // Contrast Media Administration Tracking
    public bool ContrastUsed { get; set; } = false;
    public string? ContrastAgentName { get; set; } // e.g. "Omnipaque 350 (Iohexol)", "Gadovist"
    public decimal? ContrastVolumeMl { get; set; } // Volume in mL e.g. 80.0
    public string? ContrastLotNumber { get; set; } // e.g. "LOT-98234B"
    public string? ContrastRoute { get; set; } // "IV", "Oral", "Intra-articular"

    // Attending Physician Review & Sign-Off
    public DateTime? DoctorSignedOffAt { get; set; }
    public string? DoctorSignOffNotes { get; set; }

    // Billing integration
    public decimal Price { get; set; } = 0m;
    public bool IsBilled { get; set; } = false;
}
