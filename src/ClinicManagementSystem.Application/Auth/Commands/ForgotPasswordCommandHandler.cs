using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Auth.Commands;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result<Unit>>
{
    private readonly IUserRepository _userRepository;
    private readonly IClinicDbContext _context;
    private readonly IEmailService _emailService;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IClinicDbContext context,
        IEmailService emailService)
    {
        _userRepository = userRepository;
        _context = context;
        _emailService = emailService;
    }

    public async Task<Result<Unit>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user is null)
            return Result<Unit>.Success(Unit.Value); // Don't reveal email existence

        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            Token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        };

        _context.PasswordResetTokens.Add(resetToken);
        await _context.SaveChangesAsync(cancellationToken);

        var resetLink = $"http://localhost:4200/reset-password?token={resetToken.Token}";
        await _emailService.SendPasswordResetAsync(user.Email, resetLink);

        return Result<Unit>.Success(Unit.Value);
    }
}
