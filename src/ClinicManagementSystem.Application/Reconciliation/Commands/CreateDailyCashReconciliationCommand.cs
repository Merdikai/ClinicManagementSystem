using System;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Reconciliation.Commands;

public record CreateDailyCashReconciliationCommand(
    Guid ReceptionistUserId,
    string Shift,
    decimal CashCollected,
    decimal CardCollected,
    decimal DigitalCollected,
    decimal TotalCollected,
    decimal ClosingCashCounted,
    string? Notes
) : IRequest<Result<DailyCashReconciliationResponseDto>>;

public class CreateDailyCashReconciliationCommandHandler : IRequestHandler<CreateDailyCashReconciliationCommand, Result<DailyCashReconciliationResponseDto>>
{
    private readonly IClinicDbContext _context;

    public CreateDailyCashReconciliationCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<DailyCashReconciliationResponseDto>> Handle(CreateDailyCashReconciliationCommand request, CancellationToken cancellationToken)
    {
        var discrepancy = request.ClosingCashCounted - request.CashCollected;
        var status = Math.Abs(discrepancy) < 0.01m ? "Balanced" : "Discrepancy";

        var today = DateTime.UtcNow.Date;
        var invoicesCount = await _context.Payments
            .Where(p => p.PaymentDate >= today)
            .Select(p => p.InvoiceId)
            .Distinct()
            .CountAsync(cancellationToken);

        var entity = new DailyCashReconciliation
        {
            Id = Guid.NewGuid(),
            ReceptionistUserId = request.ReceptionistUserId,
            ReconciliationDate = DateTime.UtcNow,
            Shift = request.Shift ?? "Morning",
            CashCollected = request.CashCollected,
            CardCollected = request.CardCollected,
            DigitalCollected = request.DigitalCollected,
            TotalCollected = request.TotalCollected,
            ClosingCashCounted = request.ClosingCashCounted,
            DiscrepancyAmount = discrepancy,
            InvoicesSettledCount = invoicesCount,
            Status = status,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.DailyCashReconciliations.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.ReceptionistUserId, cancellationToken);
        var userName = user != null ? $"{user.FirstName} {user.LastName}" : "Front Desk Receptionist";

        var responseDto = new DailyCashReconciliationResponseDto
        {
            Id = entity.Id,
            ReceptionistUserId = entity.ReceptionistUserId,
            ReceptionistName = userName,
            ReconciliationDate = entity.ReconciliationDate,
            Shift = entity.Shift,
            CashCollected = entity.CashCollected,
            CardCollected = entity.CardCollected,
            DigitalCollected = entity.DigitalCollected,
            TotalCollected = entity.TotalCollected,
            ClosingCashCounted = entity.ClosingCashCounted,
            DiscrepancyAmount = entity.DiscrepancyAmount,
            InvoicesSettledCount = entity.InvoicesSettledCount,
            Status = entity.Status,
            Notes = entity.Notes,
            CreatedAt = entity.CreatedAt
        };

        return Result<DailyCashReconciliationResponseDto>.Success(responseDto);
    }
}
