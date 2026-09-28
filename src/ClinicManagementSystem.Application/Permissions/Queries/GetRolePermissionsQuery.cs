using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Permissions.DTOs;
using ClinicManagementSystem.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Permissions.Queries;

public record GetRolePermissionsQuery(string? RoleName = null) : IRequest<List<RolePermissionMatrixDto>>;

public class GetRolePermissionsQueryHandler : IRequestHandler<GetRolePermissionsQuery, List<RolePermissionMatrixDto>>
{
    private readonly IClinicDbContext _context;

    public GetRolePermissionsQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<List<RolePermissionMatrixDto>> Handle(GetRolePermissionsQuery request, CancellationToken cancellationToken)
    {
        var allPerms = ClinicPermissions.All.ToList();
        var rolesQuery = _context.Roles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.RoleName))
        {
            rolesQuery = rolesQuery.Where(r => r.Name == request.RoleName.Trim());
        }

        var roles = await rolesQuery.ToListAsync(cancellationToken);
        var roleIds = roles.Select(r => r.Id).ToList();

        var dbPerms = await _context.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .ToListAsync(cancellationToken);

        var result = new List<RolePermissionMatrixDto>();
        foreach (var role in roles)
        {
            var granted = dbPerms.Where(p => p.RoleId == role.Id).Select(p => p.Permission).ToList();
            result.Add(new RolePermissionMatrixDto
            {
                RoleName = role.Name,
                GrantedPermissions = granted,
                AllAvailablePermissions = allPerms
            });
        }

        return result;
    }
}
