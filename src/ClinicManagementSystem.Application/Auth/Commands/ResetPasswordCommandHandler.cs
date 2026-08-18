using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Auth.Commands;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<Unit>>
{
    private readonly IClinicDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public ResetPasswordCommandHandler(
        IClinicDbContext context,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<Unit>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var resetToken = await _context.PasswordResetTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.Token && !rt.IsUsed && rt.ExpiresAt > DateTime.UtcNow, cancellationToken);

        if (resetToken is null)
            return Result<Unit>.Failure("Invalid or expired reset token.", "invalid_reset_token");

        var user = await _userRepository.GetByIdAsync(resetToken.UserId);
        if (user is null)
            return Result<Unit>.Failure("User not found.", "user_not_found");

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        _userRepository.Update(user);

        resetToken.IsUsed = true;
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
