using ClinicManagementSystem.Application.DTOs;
using FluentValidation;

namespace ClinicManagementSystem.Application.Validators;

public class CreateMedicineValidator : AbstractValidator<CreateMedicineDto>
{
    public CreateMedicineValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Medicine code is required")
            .MaximumLength(50).WithMessage("Code must not exceed 50 characters");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Medicine name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required")
            .MaximumLength(100).WithMessage("Category must not exceed 100 characters");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative");

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0).WithMessage("Unit price must be greater than zero")
            .PrecisionScale(18, 2, false).WithMessage("Price must have at most 2 decimal places");
    }
}
