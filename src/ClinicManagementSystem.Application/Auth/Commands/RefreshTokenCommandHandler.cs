using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Options;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ClinicManagementSystem.Application.Auth.Commands;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IClinicDbContext _context;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly JwtOptions _jwtOptions;

    public RefreshTokenCommandHandler(
        IClinicDbContext context,
        IJwtTokenGenerator tokenGenerator,
        IOptions<JwtOptions> jwtOptions)
    {
        _context = context;
        _tokenGenerator = tokenGenerator;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw new BusinessRuleViolationException("Invalid or expired refresh token.", "invalid_refresh_token");

        var tokenRecord = await _context.RefreshTokens
            .Include(r => r.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken, cancellationToken);

        if (tokenRecord is null)
            throw new BusinessRuleViolationException("Invalid or expired refresh token.", "invalid_refresh_token");

        // M11/M12 Theft Detection:
        // If an already-used or revoked refresh token is presented again, revoke the entire active family.
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

        if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > DateTime.UtcNow)
            throw new BusinessRuleViolationException("Account is locked.", "account_locked");

        tokenRecord.RevokedAt = DateTime.UtcNow;

        var newRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryDays)
        };
        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        var roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new List<string>();
        var patient = await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Email == user.Email, cancellationToken);

        return new AuthResponseDto
        {
            Token = _tokenGenerator.GenerateToken(user, roles),
            RefreshToken = newRefreshToken.Token,
            Username = user.Username,
            Email = user.Email ?? string.Empty,
            FullName = $"{user.FirstName} {user.LastName}",
            Roles = roles,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes),
            PatientId = patient?.Id
        };
    }
}
