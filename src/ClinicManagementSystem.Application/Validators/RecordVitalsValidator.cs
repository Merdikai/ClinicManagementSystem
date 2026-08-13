using ClinicManagementSystem.Application.DTOs;
using FluentValidation;

namespace ClinicManagementSystem.Application.Validators;

public class RecordVitalsValidator : AbstractValidator<RecordVitalsDto>
{
    public RecordVitalsValidator()
    {
        RuleFor(x => x.AppointmentId)
            .NotEmpty().WithMessage("Appointment ID is required");

        RuleFor(x => x.SystolicBP)
            .InclusiveBetween(50, 300).WithMessage("Systolic BP must be between 50 and 300 mmHg");

        RuleFor(x => x.DiastolicBP)
            .InclusiveBetween(30, 200).WithMessage("Diastolic BP must be between 30 and 200 mmHg");

        RuleFor(x => x.TemperatureC)
            .InclusiveBetween(30, 45).WithMessage("Temperature must be between 30°C and 45°C");

        RuleFor(x => x.HeartRateBpm)
            .InclusiveBetween(20, 300).WithMessage("Heart rate must be between 20 and 300 BPM");

        RuleFor(x => x.RespiratoryRate)
            .InclusiveBetween(5, 80).WithMessage("Respiratory rate must be between 5 and 80 breaths/min");

        RuleFor(x => x.WeightKg)
            .InclusiveBetween(0.5m, 500m).WithMessage("Weight must be between 0.5 and 500 kg");

        RuleFor(x => x.HeightCm)
            .InclusiveBetween(20, 250).WithMessage("Height must be between 20 and 250 cm");
    }
}
