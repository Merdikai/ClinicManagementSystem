using System;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Finance.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Finance.Commands;

public record AuditDailyCashReconciliationCommand(
    Guid ReconciliationId,
    AuditDailyCashReconciliationDto Dto,
    Guid AuditorUserId,
    string AuditorUserName
) : IRequest<Result<DailyCashReconciliationResponseDto>>;

public class AuditDailyCashReconciliationCommandHandler : IRequestHandler<AuditDailyCashReconciliationCommand, Result<DailyCashReconciliationResponseDto>>
{
    private readonly IClinicDbContext _context;

    public AuditDailyCashReconciliationCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<DailyCashReconciliationResponseDto>> Handle(AuditDailyCashReconciliationCommand request, CancellationToken cancellationToken)
    {
        var reconciliation = await _context.DailyCashReconciliations
            .Include(r => r.ReceptionistUser)
            .FirstOrDefaultAsync(r => r.Id == request.ReconciliationId, cancellationToken);

        if (reconciliation == null)
            return Result<DailyCashReconciliationResponseDto>.Failure("Daily cash reconciliation not found.", "reconciliation_not_found");

        reconciliation.AuditStatus = request.Dto.AuditStatus;
        reconciliation.AuditNotes = request.Dto.AuditNotes;
        reconciliation.AuditedByUserId = request.AuditorUserId;
        reconciliation.AuditedByUserName = request.AuditorUserName;
        reconciliation.AuditedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var receptionistName = reconciliation.ReceptionistUser != null
            ? $"{reconciliation.ReceptionistUser.FirstName} {reconciliation.ReceptionistUser.LastName}".Trim()
            : "Front Desk";

        return Result<DailyCashReconciliationResponseDto>.Success(new DailyCashReconciliationResponseDto
        {
            Id = reconciliation.Id,
            ReceptionistUserId = reconciliation.ReceptionistUserId,
            ReceptionistName = receptionistName,
            ReconciliationDate = reconciliation.ReconciliationDate,
            Shift = reconciliation.Shift,
            CashCollected = reconciliation.CashCollected,
            CardCollected = reconciliation.CardCollected,
            DigitalCollected = reconciliation.DigitalCollected,
            TotalCollected = reconciliation.TotalCollected,
            ClosingCashCounted = reconciliation.ClosingCashCounted,
            DiscrepancyAmount = reconciliation.DiscrepancyAmount,
            InvoicesSettledCount = reconciliation.InvoicesSettledCount,
            Status = reconciliation.Status,
            Notes = reconciliation.Notes,
            AuditedByUserId = reconciliation.AuditedByUserId,
            AuditedByUserName = reconciliation.AuditedByUserName,
            AuditedAt = reconciliation.AuditedAt,
            AuditStatus = reconciliation.AuditStatus,
            AuditNotes = reconciliation.AuditNotes,
            CreatedAt = reconciliation.CreatedAt
        });
    }
}
