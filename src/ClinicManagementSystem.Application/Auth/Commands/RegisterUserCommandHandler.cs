using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Auth.Commands;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, AuthResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserCommandHandler(IUserRepository userRepository, IJwtTokenGenerator tokenGenerator, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _tokenGenerator = tokenGenerator;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResponseDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepository.GetByUsernameAsync(request.Username) is not null)
            throw new BusinessRuleViolationException("Username already taken.", "duplicate_username");

        if (await _userRepository.GetByEmailAsync(request.Email) is not null)
            throw new BusinessRuleViolationException("Email already registered.", "duplicate_email");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        var roles = new List<string> { "Patient" };
        return new AuthResponseDto
        {
            Token = _tokenGenerator.GenerateToken(user, roles),
            Username = user.Username,
            FullName = $"{user.FirstName} {user.LastName}",
            Roles = roles
        };
    }
}
