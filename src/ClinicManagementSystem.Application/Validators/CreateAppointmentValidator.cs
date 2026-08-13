using ClinicManagementSystem.Application.DTOs;
using FluentValidation;

namespace ClinicManagementSystem.Application.Validators;

public class CreateAppointmentValidator : AbstractValidator<CreateAppointmentDto>
{
    public CreateAppointmentValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("Patient ID is required");

        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("Doctor ID is required");

        RuleFor(x => x.ScheduledDateTime)
            .NotEmpty().WithMessage("Appointment date and time is required")
            .GreaterThan(DateTime.UtcNow).WithMessage("Appointment must be in the future");

        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(10, 120).WithMessage("Duration must be between 10 and 120 minutes");

        RuleFor(x => x.ReasonForVisit)
            .NotEmpty().WithMessage("Reason for visit is required")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters");
    }
}
