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

public record GetPayrollRecordsQuery(int? PeriodMonth = null, int? PeriodYear = null) : IRequest<List<PayrollRecordDto>>;

public class GetPayrollRecordsQueryHandler : IRequestHandler<GetPayrollRecordsQuery, List<PayrollRecordDto>>
{
    private readonly IClinicDbContext _context;

    public GetPayrollRecordsQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<List<PayrollRecordDto>> Handle(GetPayrollRecordsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.PayrollRecords.AsNoTracking();

        if (request.PeriodMonth.HasValue)
        {
            query = query.Where(p => p.PeriodMonth == request.PeriodMonth.Value);
        }

        if (request.PeriodYear.HasValue)
        {
            query = query.Where(p => p.PeriodYear == request.PeriodYear.Value);
        }

        var records = await query
            .OrderByDescending(p => p.PeriodYear)
            .ThenByDescending(p => p.PeriodMonth)
            .ThenBy(p => p.StaffName)
            .ToListAsync(cancellationToken);

        return records.Select(p => new PayrollRecordDto
        {
            Id = p.Id,
            PayrollNumber = p.PayrollNumber,
            StaffUserId = p.StaffUserId,
            StaffName = p.StaffName,
            Role = p.Role,
            PeriodMonth = p.PeriodMonth,
            PeriodYear = p.PeriodYear,
            BaseSalary = p.BaseSalary,
            ConsultationsCount = p.ConsultationsCount,
            DoctorCommissionShare = p.DoctorCommissionShare,
            OvertimeHours = p.OvertimeHours,
            OvertimePay = p.OvertimePay,
            TaxDeductions = p.TaxDeductions,
            NetSalaryPayable = p.NetSalaryPayable,
            Status = p.Status,
            DisbursedAt = p.DisbursedAt,
            PaymentReference = p.PaymentReference,
            CreatedAt = p.CreatedAt
        }).ToList();
    }
}
