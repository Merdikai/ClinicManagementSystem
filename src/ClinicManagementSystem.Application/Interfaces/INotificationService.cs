namespace ClinicManagementSystem.Application.Interfaces;

public interface INotificationService
{
    Task NotifyAppointmentBookedAsync(Guid doctorId, Guid appointmentId, string patientName);
    Task NotifyPatientCheckedInAsync(Guid doctorId, Guid appointmentId, string patientName);
    Task NotifyLowStockAsync(string medicineName, int currentStock);
    Task NotifyInvoicePaidAsync(Guid patientId, Guid invoiceId, decimal amountPaid);
}
