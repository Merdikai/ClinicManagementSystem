using System;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.Finance.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Finance.Commands;

public record CreateInsuranceClaimCommand(
    CreateInsuranceClaimDto Dto
) : IRequest<Result<InsuranceClaimDto>>;

public class CreateInsuranceClaimCommandHandler : IRequestHandler<CreateInsuranceClaimCommand, Result<InsuranceClaimDto>>
{
    private readonly IClinicDbContext _context;

    public CreateInsuranceClaimCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<InsuranceClaimDto>> Handle(CreateInsuranceClaimCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Patient)
            .FirstOrDefaultAsync(i => i.Id == request.Dto.InvoiceId, cancellationToken);

        if (invoice == null)
            return Result<InsuranceClaimDto>.Failure("Invoice not found.", "invoice_not_found");

        var claimNumber = $"CLM-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";

        var claim = new InsuranceClaim
        {
            Id = Guid.NewGuid(),
            ClaimNumber = claimNumber,
            InvoiceId = invoice.Id,
            PatientId = invoice.PatientId,
            InsuranceProvider = request.Dto.InsuranceProvider,
            PolicyNumber = request.Dto.PolicyNumber,
            GroupNumber = request.Dto.GroupNumber,
            ClaimAmount = request.Dto.ClaimAmount > 0 ? request.Dto.ClaimAmount : invoice.TotalAmount,
            CoveredAmount = 0m,
            PatientCopay = request.Dto.PatientCopay,
            Status = "Submitted",
            SubmittedAt = DateTime.UtcNow,
            Notes = request.Dto.Notes
        };

        _context.InsuranceClaims.Add(claim);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<InsuranceClaimDto>.Success(new InsuranceClaimDto
        {
            Id = claim.Id,
            ClaimNumber = claim.ClaimNumber,
            InvoiceId = claim.InvoiceId,
            InvoiceNumber = invoice.InvoiceNumber,
            PatientId = claim.PatientId,
            PatientName = invoice.Patient != null ? $"{invoice.Patient.FirstName} {invoice.Patient.LastName}" : string.Empty,
            InsuranceProvider = claim.InsuranceProvider,
            PolicyNumber = claim.PolicyNumber,
            GroupNumber = claim.GroupNumber,
            ClaimAmount = claim.ClaimAmount,
            CoveredAmount = claim.CoveredAmount,
            PatientCopay = claim.PatientCopay,
            Status = claim.Status,
            SubmittedAt = claim.SubmittedAt,
            Notes = claim.Notes
        });
    }
}

public record ReconcileInsuranceClaimCommand(
    Guid ClaimId,
    ReconcileInsuranceClaimDto Dto
) : IRequest<Result<InsuranceClaimDto>>;

public class ReconcileInsuranceClaimCommandHandler : IRequestHandler<ReconcileInsuranceClaimCommand, Result<InsuranceClaimDto>>
{
    private readonly IClinicDbContext _context;

    public ReconcileInsuranceClaimCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<InsuranceClaimDto>> Handle(ReconcileInsuranceClaimCommand request, CancellationToken cancellationToken)
    {
        var claim = await _context.InsuranceClaims
            .Include(c => c.Invoice)
            .Include(c => c.Patient)
            .FirstOrDefaultAsync(c => c.Id == request.ClaimId, cancellationToken);

        if (claim == null)
            return Result<InsuranceClaimDto>.Failure("Insurance claim not found.", "claim_not_found");

        claim.Status = request.Dto.Status;
        claim.CoveredAmount = request.Dto.CoveredAmount;
        claim.RemittanceReference = request.Dto.RemittanceReference;
        claim.DenialReason = request.Dto.DenialReason;
        claim.SettledAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Dto.Notes))
        {
            claim.Notes = string.IsNullOrWhiteSpace(claim.Notes)
                ? request.Dto.Notes
                : $"{claim.Notes} | {request.Dto.Notes}";
        }

        // If claim was remitted or approved, record payment towards the invoice
        if ((request.Dto.Status == "Remitted" || request.Dto.Status == "Approved") && request.Dto.CoveredAmount > 0 && claim.Invoice != null)
        {
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                InvoiceId = claim.InvoiceId,
                AmountPaid = request.Dto.CoveredAmount,
                PaymentDate = DateTime.UtcNow,
                PaymentMethod = "Insurance Remittance (EFT)",
                TransactionReference = request.Dto.RemittanceReference ?? $"REM-{claim.ClaimNumber}"
            };
            _context.Payments.Add(payment);

            // Recalculate invoice status
            var currentTotalPaid = await _context.Payments
                .Where(p => p.InvoiceId == claim.InvoiceId)
                .SumAsync(p => p.AmountPaid, cancellationToken) + request.Dto.CoveredAmount;

            if (currentTotalPaid >= claim.Invoice.TotalAmount)
            {
                claim.Invoice.Status = ClinicManagementSystem.Domain.Enums.InvoiceStatus.Paid;
            }
            else if (currentTotalPaid > 0)
            {
                claim.Invoice.Status = ClinicManagementSystem.Domain.Enums.InvoiceStatus.PartiallyPaid;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<InsuranceClaimDto>.Success(new InsuranceClaimDto
        {
            Id = claim.Id,
            ClaimNumber = claim.ClaimNumber,
            InvoiceId = claim.InvoiceId,
            InvoiceNumber = claim.Invoice?.InvoiceNumber ?? string.Empty,
            PatientId = claim.PatientId,
            PatientName = claim.Patient != null ? $"{claim.Patient.FirstName} {claim.Patient.LastName}" : string.Empty,
            InsuranceProvider = claim.InsuranceProvider,
            PolicyNumber = claim.PolicyNumber,
            GroupNumber = claim.GroupNumber,
            ClaimAmount = claim.ClaimAmount,
            CoveredAmount = claim.CoveredAmount,
            PatientCopay = claim.PatientCopay,
            Status = claim.Status,
            RemittanceReference = claim.RemittanceReference,
            DenialReason = claim.DenialReason,
            SubmittedAt = claim.SubmittedAt,
            SettledAt = claim.SettledAt,
            Notes = claim.Notes
        });
    }
}
