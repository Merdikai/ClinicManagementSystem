using System;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Notifications.Commands;

public record SendTestNotificationCommand(Guid UserId, string Message) : IRequest<string>;

public class SendTestNotificationCommandHandler : IRequestHandler<SendTestNotificationCommand, string>
{
    private readonly INotificationService _notificationService;

    public SendTestNotificationCommandHandler(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task<string> Handle(SendTestNotificationCommand request, CancellationToken cancellationToken)
    {
        await _notificationService.NotifyAppointmentBookedAsync(
            request.UserId,
            Guid.NewGuid(),
            request.Message
        );

        return request.Message;
    }
}
