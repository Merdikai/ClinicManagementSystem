using ClinicManagementSystem.Application.DTOs;
using FluentValidation;

namespace ClinicManagementSystem.Application.Validators;

public class CreateInvoiceValidator : AbstractValidator<CreateInvoiceDto>
{
    public CreateInvoiceValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("Patient ID is required");

        RuleFor(x => x.TaxAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Tax cannot be negative")
            .PrecisionScale(18, 2, false).WithMessage("Tax must have at most 2 decimal places");

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Discount cannot be negative")
            .PrecisionScale(18, 2, false).WithMessage("Discount must have at most 2 decimal places");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one invoice item is required")
            .Must(items => items.Count <= 100).WithMessage("Maximum 100 items per invoice");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Description)
                .NotEmpty().WithMessage("Item description is required")
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than zero");

            item.RuleFor(i => i.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Unit price cannot be negative")
                .PrecisionScale(18, 2, false).WithMessage("Price must have at most 2 decimal places");
        });
    }
}
