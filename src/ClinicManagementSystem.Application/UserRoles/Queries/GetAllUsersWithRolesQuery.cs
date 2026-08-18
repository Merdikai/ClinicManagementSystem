using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.UserRoles.Queries;

public record GetAllUsersWithRolesQuery : IRequest<Result<IEnumerable<UserWithRolesDto>>>;

public class GetAllUsersWithRolesQueryHandler : IRequestHandler<GetAllUsersWithRolesQuery, Result<IEnumerable<UserWithRolesDto>>>
{
    private readonly IClinicDbContext _context;

    public GetAllUsersWithRolesQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IEnumerable<UserWithRolesDto>>> Handle(GetAllUsersWithRolesQuery request, CancellationToken cancellationToken)
    {
        var users = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .ToListAsync(cancellationToken);

        var dtos = users.Select(u => new UserWithRolesDto(
            u.Id,
            u.Username,
            $"{u.FirstName} {u.LastName}",
            u.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new List<string>()
        ));

        return Result<IEnumerable<UserWithRolesDto>>.Success(dtos);
    }
}
