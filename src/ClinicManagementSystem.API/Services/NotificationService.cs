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
        var payload = new
        {
            AppointmentId = appointmentId,
            DoctorId = doctorId,
            PatientName = patientName,
            Timestamp = DateTime.UtcNow,
            Message = $"New appointment booked for {patientName}"
        };

        await _hubContext.Clients.All.AppointmentBooked(payload);
    }

    public async Task NotifyPatientCheckedInAsync(Guid doctorId, Guid appointmentId, string patientName)
    {
        var payload = new
        {
            AppointmentId = appointmentId,
            DoctorId = doctorId,
            PatientName = patientName,
            Timestamp = DateTime.UtcNow,
            Message = $"{patientName} has checked in and is queued for consultation"
        };

        await _hubContext.Clients.All.PatientCheckedIn(payload);
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

        await _hubContext.Clients.All.LowStockAlert(payload);
    }

    public async Task NotifyInvoicePaidAsync(Guid patientId, Guid invoiceId, decimal amountPaid)
    {
        var payload = new
        {
            InvoiceId = invoiceId,
            PatientId = patientId,
            AmountPaid = amountPaid,
            Timestamp = DateTime.UtcNow,
            Message = $"Payment of {amountPaid:C} received for invoice"
        };

        await _hubContext.Clients.All.InvoicePaid(payload);
    }
}
