using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.UserRoles.Commands;

public record RemoveRoleCommand(Guid UserId, string RoleName) : IRequest<Result<Unit>>;

public class RemoveRoleCommandHandler : IRequestHandler<RemoveRoleCommand, Result<Unit>>
{
    private readonly IClinicDbContext _context;

    public RemoveRoleCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Unit>> Handle(RemoveRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == request.RoleName.ToLower(), cancellationToken);
        if (role is null)
            return Result<Unit>.Failure("Role not found", "role_not_found");

        if (role.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            var otherAdmins = await _context.UserRoles
                .CountAsync(ur => ur.RoleId == role.Id && ur.UserId != request.UserId, cancellationToken);
            if (otherAdmins == 0)
                return Result<Unit>.Failure("Cannot remove the only remaining Administrator from the system.", "cannot_remove_last_admin");
        }

        var assignment = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == request.UserId && ur.RoleId == role.Id, cancellationToken);

        if (assignment is null)
            return Result<Unit>.Failure("User does not have this role", "role_not_assigned");

        _context.UserRoles.Remove(assignment);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
