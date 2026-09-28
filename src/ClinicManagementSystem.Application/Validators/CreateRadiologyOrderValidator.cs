using ClinicManagementSystem.Application.Radiology.DTOs;
using FluentValidation;

namespace ClinicManagementSystem.Application.Validators;

public class CreateRadiologyOrderValidator : AbstractValidator<CreateRadiologyOrderDto>
{
    public CreateRadiologyOrderValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("Patient ID is required.");

        RuleFor(x => x.Modality)
            .NotEmpty().WithMessage("Modality is required.")
            .Must(m => new[] { "X-Ray", "CT Scan", "MRI", "Ultrasound", "Mammography", "Fluoroscopy" }.Contains(m))
            .WithMessage("Invalid imaging modality specified.");

        RuleFor(x => x.ProcedureName)
            .NotEmpty().WithMessage("Procedure name is required.")
            .MaximumLength(150).WithMessage("Procedure name cannot exceed 150 characters.");

        RuleFor(x => x.BodyPart)
            .NotEmpty().WithMessage("Anatomical body part is required.")
            .MaximumLength(100).WithMessage("Body part cannot exceed 100 characters.");

        RuleFor(x => x.ClinicalIndication)
            .NotEmpty().WithMessage("Clinical indication is required.")
            .MaximumLength(500).WithMessage("Clinical indication cannot exceed 500 characters.");

        RuleFor(x => x.Priority)
            .NotEmpty().WithMessage("Priority is required.")
            .Must(p => new[] { "Routine", "Urgent", "Stat" }.Contains(p))
            .WithMessage("Priority must be Routine, Urgent, or Stat.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price must be non-negative.");
    }
}
