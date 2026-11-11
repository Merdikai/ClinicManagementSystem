using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Auth.Commands;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IClinicDbContext _context;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public RefreshTokenCommandHandler(IClinicDbContext context, IJwtTokenGenerator tokenGenerator)
    {
        _context = context;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenRecord = await _context.RefreshTokens
            .Include(r => r.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken, cancellationToken);

        if (tokenRecord is null)
            throw new BusinessRuleViolationException("Invalid or expired refresh token.", "invalid_refresh_token");

        // Qiyas M11 Session 2 Theft Detection:
        // If an already-used or revoked refresh token is presented again, the token family is compromised!
        if (tokenRecord.RevokedAt is not null)
        {
            var compromisedTokens = await _context.RefreshTokens
                .Where(r => r.UserId == tokenRecord.UserId && r.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var t in compromisedTokens)
            {
                t.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            throw new SecurityAlertException("Security alert: Refresh token reuse detected. All active sessions have been revoked.", "refresh_token_reuse_detected");
        }

        if (!tokenRecord.IsActive)
            throw new BusinessRuleViolationException("Invalid or expired refresh token.", "invalid_refresh_token");

        var user = tokenRecord.User;
        if (!user.IsActive)
            throw new BusinessRuleViolationException("Account is deactivated.", "account_deactivated");

        // Revoke old refresh token and issue new one
        tokenRecord.RevokedAt = DateTime.UtcNow;

        var newRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        var roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new List<string>();

        return new AuthResponseDto
        {
            Token = _tokenGenerator.GenerateToken(user, roles),
            RefreshToken = newRefreshToken.Token,
            Username = user.Username,
            FullName = $"{user.FirstName} {user.LastName}",
            Roles = roles
        };
    }
}
