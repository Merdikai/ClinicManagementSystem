using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Prescriptions.Commands;

public record GeneratePrescriptionInvoiceCommand(Guid PrescriptionId) : IRequest<Result<InvoiceResponseDto>>;

public class GeneratePrescriptionInvoiceCommandHandler : IRequestHandler<GeneratePrescriptionInvoiceCommand, Result<InvoiceResponseDto>>
{
    private readonly IClinicDbContext _context;

    public GeneratePrescriptionInvoiceCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<InvoiceResponseDto>> Handle(GeneratePrescriptionInvoiceCommand request, CancellationToken cancellationToken)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.PrescriptionItems)
                .ThenInclude(pi => pi.Medicine)
            .Include(p => p.Consultation)
                .ThenInclude(c => c.Appointment)
                    .ThenInclude(a => a.Patient)
            .Include(p => p.Invoice!)
                .ThenInclude(i => i.LineItems)
            .Include(p => p.Invoice!)
                .ThenInclude(i => i.Payments)
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, cancellationToken);

        if (prescription is null)
            return Result<InvoiceResponseDto>.Failure("Prescription not found.", "prescription_not_found");

        var patient = prescription.Consultation?.Appointment?.Patient;
        if (patient is null)
            return Result<InvoiceResponseDto>.Failure("Patient record not found for this prescription.", "patient_not_found");

        // If an invoice is already attached, return it
        if (prescription.Invoice != null)
        {
            var existingDto = new InvoiceResponseDto
            {
                Id = prescription.Invoice.Id,
                InvoiceNumber = prescription.Invoice.InvoiceNumber,
                PatientName = $"{patient.FirstName} {patient.LastName}".Trim(),
                IssueDate = prescription.Invoice.IssueDate,
                DueDate = prescription.Invoice.DueDate,
                SubTotal = prescription.Invoice.SubTotal,
                TaxAmount = prescription.Invoice.TaxAmount,
                DiscountAmount = prescription.Invoice.DiscountAmount,
                TotalAmount = prescription.Invoice.TotalAmount,
                BalanceDue = prescription.Invoice.BalanceDue,
                Status = prescription.Invoice.Status.ToString(),
                Department = string.IsNullOrWhiteSpace(prescription.Invoice.Department) || prescription.Invoice.Department == "General" ? "Pharmacy" : prescription.Invoice.Department,
                LineItems = prescription.Invoice.LineItems.Select(li => new InvoiceItemResponseDto
                {
                    Description = li.Description,
                    Quantity = li.Quantity,
                    UnitPrice = li.UnitPrice,
                    LineTotal = li.LineTotal
                }).ToList()
            };
            return Result<InvoiceResponseDto>.Success(existingDto);
        }

        var lineItems = new List<InvoiceItem>();
        decimal subTotal = 0m;

        foreach (var item in prescription.PrescriptionItems)
        {
            var medName = item.Medicine?.Name ?? "Prescription Medication";
            var unitPrice = item.UnitPrice > 0 ? item.UnitPrice : (item.Medicine?.UnitPrice ?? 0m);
            var itemTotal = unitPrice * item.Quantity;
            subTotal += itemTotal;

            lineItems.Add(new InvoiceItem
            {
                Id = Guid.NewGuid(),
                Description = $"{medName} (Qty: {item.Quantity}) - {item.DosageInstructions ?? "Take as directed"}",
                Quantity = item.Quantity,
                UnitPrice = unitPrice
            });
        }

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = $"INV-RX-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}",
            PatientId = patient.Id,
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(15),
            SubTotal = subTotal,
            TaxAmount = 0m,
            DiscountAmount = 0m,
            TotalAmount = subTotal,
            Department = "Pharmacy",
            Status = InvoiceStatus.Unpaid
        };

        foreach (var li in lineItems)
        {
            li.InvoiceId = invoice.Id;
            invoice.LineItems.Add(li);
        }

        prescription.InvoiceId = invoice.Id;
        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = new InvoiceResponseDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            PatientName = $"{patient.FirstName} {patient.LastName}".Trim(),
            IssueDate = invoice.IssueDate,
            DueDate = invoice.DueDate,
            SubTotal = invoice.SubTotal,
            TaxAmount = invoice.TaxAmount,
            DiscountAmount = invoice.DiscountAmount,
            TotalAmount = invoice.TotalAmount,
            BalanceDue = invoice.BalanceDue,
            Status = invoice.Status.ToString(),
            Department = invoice.Department,
            LineItems = invoice.LineItems.Select(li => new InvoiceItemResponseDto
            {
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                LineTotal = li.LineTotal
            }).ToList()
        };

        return Result<InvoiceResponseDto>.Success(dto);
    }
}

