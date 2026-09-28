using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Auth.Commands;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result<string?>>
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

    public async Task<Result<string?>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user is null)
            return Result<string?>.Success(null); // Don't reveal email existence

        var priorTokens = await _context.PasswordResetTokens
            .Where(rt => rt.UserId == user.Id && !rt.IsUsed)
            .ToListAsync(cancellationToken);

        foreach (var pt in priorTokens)
        {
            pt.IsUsed = true;
        }

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

        return Result<string?>.Success(resetToken.Token);
    }
}
