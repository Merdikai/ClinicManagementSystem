namespace ClinicManagementSystem.Application.Interfaces;

public interface IClinicHubClient
{
    Task AppointmentBooked(object payload);
    Task PatientCheckedIn(object payload);
    Task LowStockAlert(object payload);
    Task InvoicePaid(object payload);
}
