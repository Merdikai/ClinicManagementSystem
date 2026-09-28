using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Permissions.Commands;

public record UpdateRolePermissionsCommand(string RoleName, List<string> Permissions) : IRequest<bool>;

public class UpdateRolePermissionsCommandHandler : IRequestHandler<UpdateRolePermissionsCommand, bool>
{
    private readonly IClinicDbContext _context;

    public UpdateRolePermissionsCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == request.RoleName.ToLower(), cancellationToken);
        if (role == null)
            throw new KeyNotFoundException($"Role '{request.RoleName}' was not found.");

        var existing = await _context.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .ToListAsync(cancellationToken);

        _context.RolePermissions.RemoveRange(existing);

        var newPerms = request.Permissions.Distinct().Select(perm => new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = role.Id,
            Permission = perm,
            GrantedAt = DateTime.UtcNow
        }).ToList();

        _context.RolePermissions.AddRange(newPerms);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
