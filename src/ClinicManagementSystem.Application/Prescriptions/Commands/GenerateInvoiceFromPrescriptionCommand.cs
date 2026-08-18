using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Prescriptions.Commands;

public record GenerateInvoiceFromPrescriptionCommand(Guid PrescriptionId) : IRequest<Result<InvoiceResponseDto>>;

public class GenerateInvoiceFromPrescriptionCommandHandler : IRequestHandler<GenerateInvoiceFromPrescriptionCommand, Result<InvoiceResponseDto>>
{
    private readonly IPrescriptionRepository _prescriptionRepository;
    private readonly IInvoiceRepository _invoiceRepository;

    public GenerateInvoiceFromPrescriptionCommandHandler(
        IPrescriptionRepository prescriptionRepository,
        IInvoiceRepository invoiceRepository)
    {
        _prescriptionRepository = prescriptionRepository;
        _invoiceRepository = invoiceRepository;
    }

    public async Task<Result<InvoiceResponseDto>> Handle(GenerateInvoiceFromPrescriptionCommand request, CancellationToken cancellationToken)
    {
        var prescription = await _prescriptionRepository.GetByIdAsync(request.PrescriptionId);
        if (prescription is null)
            return Result<InvoiceResponseDto>.Failure("Prescription not found", "prescription_not_found");

        var patientId = prescription.Consultation?.Appointment?.PatientId ?? Guid.Empty;
        var appointmentId = prescription.Consultation?.AppointmentId;

        var invoice = new Invoice
        {
            InvoiceNumber = $"INV-{DateTime.UtcNow.Year}-{new Random().Next(1000, 9999)}",
            PatientId = patientId,
            AppointmentId = appointmentId,
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            Status = InvoiceStatus.Unpaid,
            TaxAmount = 0,
            DiscountAmount = 0
        };

        foreach (var item in prescription.PrescriptionItems)
        {
            invoice.LineItems.Add(new InvoiceItem
            {
                Description = $"{item.Medicine?.Name ?? "Medicine"} ({item.DosageInstructions})",
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });
        }

        invoice.LineItems.Add(new InvoiceItem
        {
            Description = "Consultation Fee",
            Quantity = 1,
            UnitPrice = 100m
        });

        invoice.SubTotal = invoice.LineItems.Sum(i => i.LineTotal);
        invoice.TotalAmount = invoice.SubTotal;

        await _invoiceRepository.AddAsync(invoice);

        var patientName = prescription.Consultation?.Appointment?.Patient != null
            ? $"{prescription.Consultation.Appointment.Patient.FirstName} {prescription.Consultation.Appointment.Patient.LastName}"
            : "Patient";

        return Result<InvoiceResponseDto>.Success(new InvoiceResponseDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            PatientName = patientName,
            IssueDate = invoice.IssueDate,
            DueDate = invoice.DueDate,
            Status = invoice.Status.ToString(),
            SubTotal = invoice.SubTotal,
            TaxAmount = invoice.TaxAmount,
            DiscountAmount = invoice.DiscountAmount,
            TotalAmount = invoice.TotalAmount,
            BalanceDue = invoice.BalanceDue,
            LineItems = invoice.LineItems.Select(i => new InvoiceItemResponseDto
            {
                Description = i.Description,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.LineTotal
            }).ToList()
        });
    }
}
