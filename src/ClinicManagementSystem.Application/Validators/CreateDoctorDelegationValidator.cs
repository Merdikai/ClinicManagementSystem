using ClinicManagementSystem.Application.DoctorDelegations.DTOs;
using FluentValidation;

namespace ClinicManagementSystem.Application.Validators;

public class CreateDoctorDelegationValidator : AbstractValidator<CreateDoctorDelegationDto>
{
    public CreateDoctorDelegationValidator()
    {
        RuleFor(x => x.CoveringDoctorName)
            .NotEmpty().WithMessage("Covering doctor name is required.");

        RuleFor(x => x.StartDateUtc)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDateUtc)
            .NotEmpty().WithMessage("End date is required.")
            .GreaterThan(x => x.StartDateUtc).WithMessage("End date must be after the start date.");
    }
}
