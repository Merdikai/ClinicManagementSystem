using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using MediatR;

namespace ClinicManagementSystem.Application.Auth.Commands;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IClinicDbContext _context;

    public LoginCommandHandler(IUserRepository userRepository, IJwtTokenGenerator tokenGenerator, IPasswordHasher passwordHasher, IClinicDbContext context)
    {
        _userRepository = userRepository;
        _tokenGenerator = tokenGenerator;
        _passwordHasher = passwordHasher;
        _context = context;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new BusinessRuleViolationException("Invalid username or password.", "invalid_credentials");

        if (!user.IsActive)
            throw new BusinessRuleViolationException("Account is deactivated.", "account_deactivated");

        var roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new List<string>();
        var refreshToken = await GenerateRefreshTokenAsync(user.Id);

        return new AuthResponseDto
        {
            Token = _tokenGenerator.GenerateToken(user, roles),
            RefreshToken = refreshToken,
            Username = user.Username,
            FullName = $"{user.FirstName} {user.LastName}",
            Roles = roles
        };
    }

    private async Task<string> GenerateRefreshTokenAsync(Guid userId)
    {
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return refreshToken.Token;
    }
}
