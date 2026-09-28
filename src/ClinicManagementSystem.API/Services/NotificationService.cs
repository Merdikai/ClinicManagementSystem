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

    public async Task NotifyRadiologyOrderUpdatedAsync(Guid orderId, Guid patientId, string modality, string status, string message)
    {
        var payload = new
        {
            OrderId = orderId,
            PatientId = patientId,
            Modality = modality,
            Status = status,
            Timestamp = DateTime.UtcNow,
            Message = message
        };

        await _hubContext.Clients.All.RadiologyOrderUpdated(payload);
    }

    public async Task NotifyLabOrderUpdatedAsync(Guid orderId, Guid patientId, string orderNumber, string status, bool hasCriticalFlag)
    {
        var payload = new
        {
            OrderId = orderId,
            PatientId = patientId,
            OrderNumber = orderNumber,
            Status = status,
            HasCriticalFlag = hasCriticalFlag,
            Timestamp = DateTime.UtcNow,
            Message = hasCriticalFlag ? $"CRITICAL ALERT: Lab order {orderNumber} has critical abnormal findings" : $"Lab order {orderNumber} status changed to {status}"
        };

        await _hubContext.Clients.All.LabOrderUpdated(payload);
    }

    public async Task NotifyBreakGlassAlertAsync(Guid logId, Guid patientId, Guid userId, string reason)
    {
        var payload = new
        {
            LogId = logId,
            PatientId = patientId,
            UserId = userId,
            Reason = reason,
            Timestamp = DateTime.UtcNow,
            Message = $"EMERGENCY ACCESS: Break-glass access logged on patient record. Reason: {reason}"
        };

        await _hubContext.Clients.All.BreakGlassAlert(payload);
    }

    public async Task NotifyDutyRosterChangedAsync(Guid staffId, string role, bool isOnDuty)
    {
        var payload = new
        {
            StaffId = staffId,
            Role = role,
            IsOnDuty = isOnDuty,
            Timestamp = DateTime.UtcNow,
            Message = $"Staff duty status updated: {role} is now {(isOnDuty ? "On Duty" : "Off Duty")}"
        };

        await _hubContext.Clients.All.DutyRosterChanged(payload);
    }
}
