using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.UserRoles.Commands;

/// <summary>
/// Allows an Administrator to directly reset any staff member's password
/// without requiring a self-service email flow.
/// </summary>
public record AdminResetPasswordCommand(Guid UserId, string NewPassword) : IRequest<Result<Unit>>;

public class AdminResetPasswordCommandHandler : IRequestHandler<AdminResetPasswordCommand, Result<Unit>>
{
    private readonly IClinicDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public AdminResetPasswordCommandHandler(IClinicDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<Unit>> Handle(AdminResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            return Result<Unit>.Failure("User not found.", "user_not_found");

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            return Result<Unit>.Failure("Password must be at least 8 characters.", "password_too_short");

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
