using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Finance.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Finance.Queries;

public record GetInsuranceClaimsQuery(string? Status = null, string? Provider = null) : IRequest<List<InsuranceClaimDto>>;

public class GetInsuranceClaimsQueryHandler : IRequestHandler<GetInsuranceClaimsQuery, List<InsuranceClaimDto>>
{
    private readonly IClinicDbContext _context;

    public GetInsuranceClaimsQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<List<InsuranceClaimDto>> Handle(GetInsuranceClaimsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.InsuranceClaims
            .Include(c => c.Invoice)
            .Include(c => c.Patient)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(c => c.Status == request.Status);
        }

        if (!string.IsNullOrWhiteSpace(request.Provider))
        {
            query = query.Where(c => c.InsuranceProvider.ToLower().Contains(request.Provider.ToLower()));
        }

        var claims = await query
            .OrderByDescending(c => c.SubmittedAt)
            .ToListAsync(cancellationToken);

        return claims.Select(c => new InsuranceClaimDto
        {
            Id = c.Id,
            ClaimNumber = c.ClaimNumber,
            InvoiceId = c.InvoiceId,
            InvoiceNumber = c.Invoice?.InvoiceNumber ?? string.Empty,
            PatientId = c.PatientId,
            PatientName = c.Patient != null ? $"{c.Patient.FirstName} {c.Patient.LastName}" : string.Empty,
            InsuranceProvider = c.InsuranceProvider,
            PolicyNumber = c.PolicyNumber,
            GroupNumber = c.GroupNumber,
            ClaimAmount = c.ClaimAmount,
            CoveredAmount = c.CoveredAmount,
            PatientCopay = c.PatientCopay,
            Status = c.Status,
            RemittanceReference = c.RemittanceReference,
            DenialReason = c.DenialReason,
            SubmittedAt = c.SubmittedAt,
            SettledAt = c.SettledAt,
            Notes = c.Notes
        }).ToList();
    }
}
