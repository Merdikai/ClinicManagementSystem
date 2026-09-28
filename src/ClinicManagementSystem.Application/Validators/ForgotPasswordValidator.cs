using ClinicManagementSystem.Application.DTOs;
using FluentValidation;

namespace ClinicManagementSystem.Application.Validators;

public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordDto>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("A valid email address is required");
    }
}
