using ClinicManagementSystem.Application.StaffDuty.DTOs;
using FluentValidation;

namespace ClinicManagementSystem.Application.Validators;

public class ClockInDutyValidator : AbstractValidator<ClockInDutyDto>
{
    public ClockInDutyValidator()
    {
        RuleFor(x => x.Department)
            .MaximumLength(100).WithMessage("Department cannot exceed 100 characters.");

        RuleFor(x => x.ShiftType)
            .MaximumLength(100).WithMessage("Shift type cannot exceed 100 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters.");
    }
}
