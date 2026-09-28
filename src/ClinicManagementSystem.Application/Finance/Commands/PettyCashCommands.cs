using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.Finance.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Finance.Commands;

public record CreatePettyCashVoucherCommand(
    CreatePettyCashVoucherDto Dto,
    Guid DisbursedByUserId,
    string DisbursedByUserName
) : IRequest<Result<PettyCashVoucherDto>>;

public class CreatePettyCashVoucherCommandHandler : IRequestHandler<CreatePettyCashVoucherCommand, Result<PettyCashVoucherDto>>
{
    private readonly IClinicDbContext _context;

    public CreatePettyCashVoucherCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PettyCashVoucherDto>> Handle(CreatePettyCashVoucherCommand request, CancellationToken cancellationToken)
    {
        if (request.Dto.Amount <= 0)
            return Result<PettyCashVoucherDto>.Failure("Voucher amount must be greater than zero.", "invalid_amount");

        var latestVoucher = await _context.PettyCashVouchers
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var currentBalance = latestVoucher?.RemainingBalance ?? 500.00m;

        decimal newBalance;
        if (request.Dto.Type.Equals("Replenishment", StringComparison.OrdinalIgnoreCase))
        {
            newBalance = currentBalance + request.Dto.Amount;
        }
        else
        {
            if (currentBalance < request.Dto.Amount)
            {
                return Result<PettyCashVoucherDto>.Failure($"Insufficient petty cash balance (${currentBalance:F2}) for this disbursement.", "insufficient_balance");
            }
            newBalance = currentBalance - request.Dto.Amount;
        }

        var voucherNumber = $"PCV-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";

        var voucher = new PettyCashVoucher
        {
            Id = Guid.NewGuid(),
            VoucherNumber = voucherNumber,
            Type = request.Dto.Type,
            Amount = request.Dto.Amount,
            Category = request.Dto.Category,
            Payee = request.Dto.Payee,
            Description = request.Dto.Description,
            DisbursedByUserId = request.DisbursedByUserId,
            DisbursedByUserName = request.DisbursedByUserName,
            RemainingBalance = newBalance,
            CreatedAt = DateTime.UtcNow,
            ReceiptRef = request.Dto.ReceiptRef
        };

        _context.PettyCashVouchers.Add(voucher);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<PettyCashVoucherDto>.Success(new PettyCashVoucherDto
        {
            Id = voucher.Id,
            VoucherNumber = voucher.VoucherNumber,
            Type = voucher.Type,
            Amount = voucher.Amount,
            Category = voucher.Category,
            Payee = voucher.Payee,
            Description = voucher.Description,
            DisbursedByUserId = voucher.DisbursedByUserId,
            DisbursedByUserName = voucher.DisbursedByUserName,
            RemainingBalance = voucher.RemainingBalance,
            CreatedAt = voucher.CreatedAt,
            ReceiptRef = voucher.ReceiptRef
        });
    }
}
