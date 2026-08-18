using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.UserRoles.Queries;

public record GetUserRolesQuery(Guid UserId) : IRequest<Result<UserWithRolesDto>>;

public class GetUserRolesQueryHandler : IRequestHandler<GetUserRolesQuery, Result<UserWithRolesDto>>
{
    private readonly IClinicDbContext _context;

    public GetUserRolesQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<UserWithRolesDto>> Handle(GetUserRolesQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            return Result<UserWithRolesDto>.Failure("User not found", "user_not_found");

        var roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new List<string>();

        return Result<UserWithRolesDto>.Success(new UserWithRolesDto(
            user.Id,
            user.Username,
            $"{user.FirstName} {user.LastName}",
            roles
        ));
    }
}
