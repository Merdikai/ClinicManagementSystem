using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Application.Interfaces;  // ← Interface namespaces
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;

// ❌ REMOVE this line: using ClinicManagementSystem.Infrastructure.Identity;

namespace ClinicManagementSystem.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _tokenGenerator;   // ← Interface
    private readonly IPasswordHasher _passwordHasher;       // ← Interface

    public AuthService(
        IUserRepository userRepository,
        IJwtTokenGenerator tokenGenerator,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _tokenGenerator = tokenGenerator;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterUserDto dto)
    {
        var existingUser = await _userRepository.GetByUsernameAsync(dto.Username);
        if (existingUser is not null)
            throw new BusinessRuleViolationException("Username already taken.", "duplicate_username");

        var existingEmail = await _userRepository.GetByEmailAsync(dto.Email);
        if (existingEmail is not null)
            throw new BusinessRuleViolationException("Email already registered.", "duplicate_email");

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PhoneNumber = dto.PhoneNumber
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();  // ← ADD THIS LINE - Saves to database!

        var roles = new List<string> { "Patient" };

        return new AuthResponseDto
{
    Token = _tokenGenerator.GenerateToken(user, roles),
    Username = user.Username,
    FullName = $"{user.FirstName} {user.LastName}",
    Roles = roles
};
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _userRepository.GetByUsernameAsync(dto.Username);
        if (user is null || !_passwordHasher.Verify(dto.Password, user.PasswordHash))
            throw new BusinessRuleViolationException("Invalid username or password.", "invalid_credentials");

        if (!user.IsActive)
            throw new BusinessRuleViolationException("Account is deactivated.", "account_deactivated");

        var roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new List<string>();

        return new AuthResponseDto
{
    Token = _tokenGenerator.GenerateToken(user, roles),
    Username = user.Username,
    FullName = $"{user.FirstName} {user.LastName}",
    Roles = roles
};
    }
}