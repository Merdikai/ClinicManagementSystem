namespace ClinicManagementSystem.Application.Interfaces;

public interface INotificationService
{
    Task NotifyAppointmentBookedAsync(Guid doctorId, Guid appointmentId, string patientName);
    Task NotifyPatientCheckedInAsync(Guid doctorId, Guid appointmentId, string patientName);
    Task NotifyLowStockAsync(string medicineName, int currentStock);
    Task NotifyInvoicePaidAsync(Guid patientId, Guid invoiceId, decimal amountPaid);
    Task NotifyRadiologyOrderUpdatedAsync(Guid orderId, Guid patientId, string modality, string status, string message);
    Task NotifyLabOrderUpdatedAsync(Guid orderId, Guid patientId, string orderNumber, string status, bool hasCriticalFlag);
    Task NotifyBreakGlassAlertAsync(Guid logId, Guid patientId, Guid userId, string reason);
    Task NotifyDutyRosterChangedAsync(Guid staffId, string role, bool isOnDuty);
}
