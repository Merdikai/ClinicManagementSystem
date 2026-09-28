using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.UserRoles.Commands;

public record ToggleUserStatusCommand(Guid UserId) : IRequest<Result<bool>>;

public class ToggleUserStatusCommandHandler : IRequestHandler<ToggleUserStatusCommand, Result<bool>>
{
    private readonly IClinicDbContext _context;

    public ToggleUserStatusCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(ToggleUserStatusCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
        if (user == null)
        {
            return Result<bool>.Failure($"User with ID '{request.UserId}' was not found.", "user_not_found");
        }

        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(user.IsActive);
    }
}
