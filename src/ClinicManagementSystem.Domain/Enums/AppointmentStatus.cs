namespace ClinicManagementSystem.Domain.Enums;

public enum AppointmentStatus
{
    PendingApproval,
    Scheduled,
    CheckedIn,
    TriageComplete,
    InConsultation,
    InProcedure,
    ReadyForDischarge,
    Completed,
    Cancelled,
    Rejected,
    NoShow
}
