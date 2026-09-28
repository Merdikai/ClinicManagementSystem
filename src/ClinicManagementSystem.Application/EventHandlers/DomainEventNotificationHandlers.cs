using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Common;
using ClinicManagementSystem.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Application.EventHandlers;

public record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : IDomainEvent;

public class DomainEventNotificationHandlers :
    INotificationHandler<DomainEventNotification<PatientRegisteredEvent>>,
    INotificationHandler<DomainEventNotification<AppointmentBookedEvent>>,
    INotificationHandler<DomainEventNotification<InvoicePaidEvent>>,
    INotificationHandler<DomainEventNotification<LowStockDetectedEvent>>,
    INotificationHandler<DomainEventNotification<RadiologyOrderCompletedEvent>>,
    INotificationHandler<DomainEventNotification<LabOrderCompletedEvent>>,
    INotificationHandler<DomainEventNotification<BreakGlassAccessLoggedEvent>>,
    INotificationHandler<DomainEventNotification<StaffDutyStatusChangedEvent>>,
    INotificationHandler<DomainEventNotification<DoctorDelegationCreatedEvent>>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<DomainEventNotificationHandlers> _logger;

    public DomainEventNotificationHandlers(
        INotificationService notificationService,
        ILogger<DomainEventNotificationHandlers> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<PatientRegisteredEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation("DomainEvent: Patient {FullName} registered with MRN {MRN}", e.FullName, e.MedicalRecordNumber);
        return Task.CompletedTask;
    }

    public async Task Handle(DomainEventNotification<AppointmentBookedEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation("DomainEvent: Appointment {AppointmentId} booked for Doctor {DoctorId}", e.AppointmentId, e.DoctorId);
        await _notificationService.NotifyAppointmentBookedAsync(e.DoctorId, e.AppointmentId, "Patient");
    }

    public async Task Handle(DomainEventNotification<InvoicePaidEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation("DomainEvent: Invoice {InvoiceId} paid {Amount:C}", e.InvoiceId, e.AmountPaid);
        await _notificationService.NotifyInvoicePaidAsync(e.PatientId, e.InvoiceId, e.AmountPaid);
    }

    public async Task Handle(DomainEventNotification<LowStockDetectedEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        _logger.LogWarning("DomainEvent: Low stock for {MedicineName} ({CurrentStock} remaining)", e.MedicineName, e.CurrentStock);
        await _notificationService.NotifyLowStockAsync(e.MedicineName, e.CurrentStock);
    }

    public async Task Handle(DomainEventNotification<RadiologyOrderCompletedEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation("DomainEvent: Radiology order {OrderId} completed for Patient {PatientId} ({Modality})", e.OrderId, e.PatientId, e.Modality);
        await _notificationService.NotifyRadiologyOrderUpdatedAsync(e.OrderId, e.PatientId, e.Modality, "Completed", $"Radiology {e.Modality} reported with findings");
    }

    public async Task Handle(DomainEventNotification<LabOrderCompletedEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation("DomainEvent: Lab order {OrderNumber} ({OrderId}) completed. CriticalFlag={Critical}", e.OrderNumber, e.OrderId, e.HasCriticalAbnormalities);
        await _notificationService.NotifyLabOrderUpdatedAsync(e.OrderId, e.PatientId, e.OrderNumber, e.Status, e.HasCriticalAbnormalities);
    }

    public async Task Handle(DomainEventNotification<BreakGlassAccessLoggedEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        _logger.LogWarning("SECURITY ALERT: Break-glass emergency access log {LogId} on Patient {PatientId} by User {UserId}. Reason: {Reason}",
            e.LogId, e.PatientId, e.UserId, e.Reason);
        await _notificationService.NotifyBreakGlassAlertAsync(e.LogId, e.PatientId, e.UserId, e.Reason);
    }

    public async Task Handle(DomainEventNotification<StaffDutyStatusChangedEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation("DomainEvent: Staff {StaffId} ({Role}) duty status changed. IsOnDuty={IsOnDuty}", e.StaffId, e.Role, e.IsOnDuty);
        await _notificationService.NotifyDutyRosterChangedAsync(e.StaffId, e.Role, e.IsOnDuty);
    }

    public Task Handle(DomainEventNotification<DoctorDelegationCreatedEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation("DomainEvent: Doctor delegation {DelegationId} granted from Doctor {DelegatorId} to {DelegateeId} until {EndTime}",
            e.DelegationId, e.DelegatorDoctorId, e.DelegateeDoctorId, e.EndTime);
        return Task.CompletedTask;
    }
}
