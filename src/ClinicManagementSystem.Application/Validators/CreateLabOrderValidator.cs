using ClinicManagementSystem.Application.Laboratories.DTOs;
using FluentValidation;

namespace ClinicManagementSystem.Application.Validators;

public class CreateLabOrderValidator : AbstractValidator<CreateLabOrderRequest>
{
    public CreateLabOrderValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("Patient ID is required.");

        RuleFor(x => x.Priority)
            .NotEmpty().WithMessage("Priority is required.")
            .Must(p => new[] { "Routine", "Urgent", "Stat" }.Contains(p))
            .WithMessage("Priority must be Routine, Urgent, or Stat.");

        RuleFor(x => x.TestTemplateIds)
            .NotEmpty().WithMessage("At least one test template must be selected.")
            .Must(t => t != null && t.Count > 0).WithMessage("At least one test template must be selected.");
    }
}
