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

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IClinicDbContext _context;
    private readonly JwtOptions _jwtOptions;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IJwtTokenGenerator tokenGenerator,
        IPasswordHasher passwordHasher,
        IClinicDbContext context,
        IOptions<JwtOptions> jwtOptions)
    {
        _userRepository = userRepository;
        _tokenGenerator = tokenGenerator;
        _passwordHasher = passwordHasher;
        _context = context;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username);
        if (user is null && request.Username.Contains('@'))
        {
            user = await _userRepository.GetByEmailAsync(request.Username);
        }

        if (user is not null && user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > DateTime.UtcNow)
        {
            throw new BusinessRuleViolationException(
                $"Account is temporarily locked due to multiple failed login attempts. Try again after {user.LockoutEndUtc.Value:yyyy-MM-dd HH:mm} UTC.",
                "account_locked");
        }

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            if (user is not null)
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= MaxFailedAttempts)
                {
                    user.LockoutEndUtc = DateTime.UtcNow.Add(LockoutDuration);
                    user.FailedLoginAttempts = 0;
                }

                await _context.SaveChangesAsync(cancellationToken);
            }

            throw new BusinessRuleViolationException("Invalid username or password.", "invalid_credentials");
        }

        if (!user.IsActive)
            throw new BusinessRuleViolationException("Account is deactivated.", "account_deactivated");

        if (user.FailedLoginAttempts > 0 || user.LockoutEndUtc.HasValue)
        {
            user.FailedLoginAttempts = 0;
            user.LockoutEndUtc = null;
        }

        var roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new List<string>();
        var patient = await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Email == user.Email, cancellationToken);
        var refreshToken = await GenerateRefreshTokenAsync(user.Id, cancellationToken);

        return new AuthResponseDto
        {
            Token = _tokenGenerator.GenerateToken(user, roles),
            RefreshToken = refreshToken,
            Username = user.Username,
            Email = user.Email ?? string.Empty,
            FullName = $"{user.FirstName} {user.LastName}",
            Roles = roles,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes),
            PatientId = patient?.Id
        };
    }

    private async Task<string> GenerateRefreshTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryDays)
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        return refreshToken.Token;
    }
}
