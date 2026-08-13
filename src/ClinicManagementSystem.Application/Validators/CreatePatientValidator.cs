using ClinicManagementSystem.Application.DTOs;
using FluentValidation;

namespace ClinicManagementSystem.Application.Validators;

public class CreatePatientValidator : AbstractValidator<CreatePatientDto>
{
    public CreatePatientValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required")
            .LessThan(DateTime.UtcNow).WithMessage("Date of birth must be in the past")
            .GreaterThan(DateTime.UtcNow.AddYears(-150)).WithMessage("Invalid date of birth");

        RuleFor(x => x.Gender)
            .NotEmpty().WithMessage("Gender is required")
            .Must(g => g is "Male" or "Female" or "Other")
            .WithMessage("Gender must be Male, Female, or Other");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required")
            .MaximumLength(20).WithMessage("Phone must not exceed 20 characters");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email address")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.BloodGroup)
            .Must(bg => new[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" }.Contains(bg))
            .WithMessage("Invalid blood group")
            .When(x => !string.IsNullOrEmpty(x.BloodGroup));
    }
}
