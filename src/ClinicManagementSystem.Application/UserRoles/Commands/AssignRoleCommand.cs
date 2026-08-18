using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.UserRoles.Commands;

public record AssignRoleCommand(Guid UserId, string RoleName) : IRequest<Result<Unit>>;

public class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand, Result<Unit>>
{
    private readonly IClinicDbContext _context;

    public AssignRoleCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Unit>> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null)
            return Result<Unit>.Failure("User not found", "user_not_found");

        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == request.RoleName, cancellationToken);
        if (role is null)
            return Result<Unit>.Failure("Role not found", "role_not_found");

        var existingAssignment = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == request.UserId && ur.RoleId == role.Id, cancellationToken);

        if (existingAssignment is not null)
            return Result<Unit>.Failure("User already has this role", "duplicate_role");

        _context.UserRoles.Add(new UserRole
        {
            UserId = request.UserId,
            RoleId = role.Id
        });

        await _context.SaveChangesAsync(cancellationToken);
        return Result<Unit>.Success(Unit.Value);
    }
}
