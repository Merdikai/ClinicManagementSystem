using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Laboratories.Commands;

public record GenerateLabInvoiceCommand(Guid LabOrderId) : IRequest<Result<InvoiceResponseDto>>;

public class GenerateLabInvoiceCommandHandler : IRequestHandler<GenerateLabInvoiceCommand, Result<InvoiceResponseDto>>
{
    private readonly IClinicDbContext _context;

    public GenerateLabInvoiceCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<InvoiceResponseDto>> Handle(GenerateLabInvoiceCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.LabOrders
            .Include(o => o.Patient)
            .Include(o => o.Items)
                .ThenInclude(i => i.LabTestTemplate)
            .FirstOrDefaultAsync(o => o.Id == request.LabOrderId, cancellationToken);

        if (order is null)
            return Result<InvoiceResponseDto>.Failure("Laboratory order not found.", "lab_order_not_found");

        var patient = order.Patient;
        if (patient is null)
            return Result<InvoiceResponseDto>.Failure("Patient record not found for this laboratory order.", "patient_not_found");

        // Check if an existing invoice exists for this lab order
        var existingInvoice = await _context.Invoices
            .Include(i => i.LineItems)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.BillingNotes != null && i.BillingNotes.Contains(order.OrderNumber), cancellationToken);

        if (existingInvoice != null)
        {
            var existingDto = new InvoiceResponseDto
            {
                Id = existingInvoice.Id,
                InvoiceNumber = existingInvoice.InvoiceNumber,
                PatientName = $"{patient.FirstName} {patient.LastName}".Trim(),
                IssueDate = existingInvoice.IssueDate,
                DueDate = existingInvoice.DueDate,
                SubTotal = existingInvoice.SubTotal,
                TaxAmount = existingInvoice.TaxAmount,
                DiscountAmount = existingInvoice.DiscountAmount,
                TotalAmount = existingInvoice.TotalAmount,
                BalanceDue = existingInvoice.BalanceDue,
                Status = existingInvoice.Status.ToString(),
                Department = string.IsNullOrWhiteSpace(existingInvoice.Department) || existingInvoice.Department == "General" ? "Laboratory" : existingInvoice.Department,
                LineItems = existingInvoice.LineItems.Select(li => new InvoiceItemResponseDto
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

        foreach (var item in order.Items)
        {
            var testName = item.LabTestTemplate?.TestName ?? "Diagnostic Pathology Test";
            var testCode = item.LabTestTemplate?.TestCode ?? "LAB";
            var unitPrice = item.Price > 0 ? item.Price : (item.LabTestTemplate?.Price ?? 25.00m);
            subTotal += unitPrice;

            lineItems.Add(new InvoiceItem
            {
                Id = Guid.NewGuid(),
                Description = $"[{testCode}] {testName} - Diagnostic Analysis",
                Quantity = 1,
                UnitPrice = unitPrice
            });
        }

        // If no line items, fallback to order total cost or nominal fee
        if (lineItems.Count == 0)
        {
            var cost = order.TotalCost > 0 ? order.TotalCost : 35.00m;
            subTotal = cost;
            lineItems.Add(new InvoiceItem
            {
                Id = Guid.NewGuid(),
                Description = $"Laboratory Diagnostic Testing Panel ({order.OrderNumber})",
                Quantity = 1,
                UnitPrice = cost
            });
        }

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = $"INV-LAB-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}",
            PatientId = patient.Id,
            AppointmentId = order.AppointmentId,
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(15),
            SubTotal = subTotal,
            TaxAmount = 0m,
            DiscountAmount = 0m,
            TotalAmount = subTotal,
            Status = InvoiceStatus.Unpaid,
            Department = "Laboratory",
            BillingNotes = $"Generated for Pathology Lab Order {order.OrderNumber}",
            LineItems = lineItems
        };

        _context.Invoices.Add(invoice);
        order.IsBilled = true;
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
