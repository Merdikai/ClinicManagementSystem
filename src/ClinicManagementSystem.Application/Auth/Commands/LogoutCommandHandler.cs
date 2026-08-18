using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Auth.Commands;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
{
    private readonly IClinicDbContext _context;

    public LogoutCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenRecord = await _context.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken, cancellationToken);

        if (tokenRecord is not null && tokenRecord.IsActive)
        {
            tokenRecord.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
