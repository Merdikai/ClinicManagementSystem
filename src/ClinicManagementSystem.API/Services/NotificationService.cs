using ClinicManagementSystem.API.Hubs;
using ClinicManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace ClinicManagementSystem.API.Services;

public class NotificationService : INotificationService
{
    private readonly IHubContext<ClinicHub, IClinicHubClient> _hubContext;

    public NotificationService(IHubContext<ClinicHub, IClinicHubClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyAppointmentBookedAsync(Guid doctorId, Guid appointmentId, string patientName)
    {
        await _hubContext.Clients.Group($"user-{doctorId}").AppointmentBooked(
            new
            {
                AppointmentId = appointmentId,
                PatientName = patientName,
                Timestamp = DateTime.UtcNow,
                Message = $"New appointment booked by {patientName}"
            }
        );
    }

    public async Task NotifyPatientCheckedInAsync(Guid doctorId, Guid appointmentId, string patientName)
    {
        await _hubContext.Clients.Group($"user-{doctorId}").PatientCheckedIn(
            new
            {
                AppointmentId = appointmentId,
                PatientName = patientName,
                Timestamp = DateTime.UtcNow,
                Message = $"{patientName} has checked in and is ready for consultation"
            }
        );
    }

    public async Task NotifyLowStockAsync(string medicineName, int currentStock)
    {
        var payload = new
        {
            MedicineName = medicineName,
            CurrentStock = currentStock,
            Timestamp = DateTime.UtcNow,
            Message = $"Low stock alert: {medicineName} has only {currentStock} units left"
        };

        await _hubContext.Clients.Group("Pharmacist").LowStockAlert(payload);
        await _hubContext.Clients.Group("Admin").LowStockAlert(payload);
    }

    public async Task NotifyInvoicePaidAsync(Guid patientId, Guid invoiceId, decimal amountPaid)
    {
        await _hubContext.Clients.Group($"patient-{patientId}").InvoicePaid(
            new
            {
                InvoiceId = invoiceId,
                AmountPaid = amountPaid,
                Timestamp = DateTime.UtcNow,
                Message = $"Payment of {amountPaid:C} received for invoice {invoiceId}"
            }
        );
    }
}
