namespace ClinicManagementSystem.Application.Interfaces;

public interface IClinicHubClient
{
    Task AppointmentBooked(object payload);
    Task PatientCheckedIn(object payload);
    Task LowStockAlert(object payload);
    Task InvoicePaid(object payload);
    Task RadiologyOrderUpdated(object payload);
    Task LabOrderUpdated(object payload);
    Task BreakGlassAlert(object payload);
    Task DutyRosterChanged(object payload);
}
