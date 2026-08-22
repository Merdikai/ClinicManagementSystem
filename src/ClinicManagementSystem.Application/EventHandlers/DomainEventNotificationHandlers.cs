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
    INotificationHandler<DomainEventNotification<LowStockDetectedEvent>>
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
}
