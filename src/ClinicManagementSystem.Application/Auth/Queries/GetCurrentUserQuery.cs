using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Auth.Queries;

public record GetCurrentUserQuery(Guid UserId) : IRequest<UserResponseDto?>;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserResponseDto?>
{
    private readonly IClinicDbContext _context;

    public GetCurrentUserQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<UserResponseDto?> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        return user is null ? null : new UserResponseDto(
            user.Id,
            user.Username,
            user.Email,
            user.FirstName,
            user.LastName,
            user.PhoneNumber
        );
    }
}
