using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.UserRoles.Commands;

/// <summary>
/// Atomically creates a new staff account and assigns the specified role in a single transaction.
/// This replaces the two-step public registration + role-assign workflow for Admin provisioning.
/// </summary>
public record AdminCreateUserCommand(
    string UserName,
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string RoleName,
    string? PhoneNumber) : IRequest<Result<Guid>>;

public class AdminCreateUserCommandHandler : IRequestHandler<AdminCreateUserCommand, Result<Guid>>
{
    private readonly IClinicDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public AdminCreateUserCommandHandler(IClinicDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<Guid>> Handle(AdminCreateUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Uniqueness checks (case-insensitive)
        bool usernameTaken = await _context.Users
            .AnyAsync(u => u.Username.ToLower() == request.UserName.ToLower(), cancellationToken);
        if (usernameTaken)
            return Result<Guid>.Failure("Username is already taken.", "duplicate_username");

        bool emailTaken = await _context.Users
            .AnyAsync(u => u.Email.ToLower() == request.Email.ToLower(), cancellationToken);
        if (emailTaken)
            return Result<Guid>.Failure("Email is already registered.", "duplicate_email");

        // 2. Resolve target role (case-insensitive)
        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name.ToLower() == request.RoleName.ToLower(), cancellationToken);
        if (role is null)
            return Result<Guid>.Failure($"Role '{request.RoleName}' does not exist.", "role_not_found");

        // 3. Create user with hashed password
        var user = new User
        {
            Username = request.UserName,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber ?? string.Empty,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        // 4. Assign the requested role atomically
        _context.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        });

        await _context.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(user.Id);
    }
}
