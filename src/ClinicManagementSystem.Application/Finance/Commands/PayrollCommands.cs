using System;
using System.Collections.Generic;
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

public record GenerateMonthlyPayrollCommand(
    int PeriodMonth,
    int PeriodYear
) : IRequest<Result<List<PayrollRecordDto>>>;

public class GenerateMonthlyPayrollCommandHandler : IRequestHandler<GenerateMonthlyPayrollCommand, Result<List<PayrollRecordDto>>>
{
    private readonly IClinicDbContext _context;

    public GenerateMonthlyPayrollCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<PayrollRecordDto>>> Handle(GenerateMonthlyPayrollCommand request, CancellationToken cancellationToken)
    {
        // Check if payroll records already exist for this period
        var existing = await _context.PayrollRecords
            .Where(p => p.PeriodMonth == request.PeriodMonth && p.PeriodYear == request.PeriodYear)
            .ToListAsync(cancellationToken);

        if (existing.Any())
        {
            return Result<List<PayrollRecordDto>>.Failure($"Payroll records for {request.PeriodMonth}/{request.PeriodYear} have already been generated.", "payroll_already_exists");
        }

        var users = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => u.IsActive && u.UserRoles.Any(r => r.Role.Name != "Patient"))
            .ToListAsync(cancellationToken);

        var monthStart = new DateTime(request.PeriodYear, request.PeriodMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var generatedRecords = new List<PayrollRecord>();

        foreach (var user in users)
        {
            var roleName = user.UserRoles.FirstOrDefault()?.Role.Name ?? "Staff";
            
            decimal baseSalary = roleName switch
            {
                "Doctor" => 7500.00m,
                "Nurse" => 4200.00m,
                "Pharmacist" => 5000.00m,
                "Accountant" => 4800.00m,
                "Receptionist" => 3600.00m,
                "LabTechnician" => 4000.00m,
                "Admin" => 6500.00m,
                _ => 3500.00m
            };

            int consultCount = 0;
            decimal doctorCommission = 0m;

            if (roleName == "Doctor")
            {
                consultCount = await _context.Consultations
                    .CountAsync(c => c.DoctorId == user.Id && c.ConsultedAt >= monthStart && c.ConsultedAt < monthEnd, cancellationToken);
                doctorCommission = consultCount * 20.00m; // $20 incentive per consultation
            }

            decimal overtimeHours = (roleName == "Nurse" || roleName == "Receptionist") ? 6.0m : 0m;
            decimal overtimePay = overtimeHours * 35.00m;

            decimal grossPay = baseSalary + doctorCommission + overtimePay;
            decimal taxDeduction = Math.Round(grossPay * 0.15m, 2);
            decimal netPay = grossPay - taxDeduction;

            var prNumber = $"PAY-{request.PeriodYear}{request.PeriodMonth:D2}-{user.Username.ToUpper()}";

            var record = new PayrollRecord
            {
                Id = Guid.NewGuid(),
                PayrollNumber = prNumber,
                StaffUserId = user.Id,
                StaffName = $"{user.FirstName} {user.LastName}".Trim(),
                Role = roleName,
                PeriodMonth = request.PeriodMonth,
                PeriodYear = request.PeriodYear,
                BaseSalary = baseSalary,
                ConsultationsCount = consultCount,
                DoctorCommissionShare = doctorCommission,
                OvertimeHours = overtimeHours,
                OvertimePay = overtimePay,
                TaxDeductions = taxDeduction,
                NetSalaryPayable = netPay,
                Status = "Draft",
                CreatedAt = DateTime.UtcNow
            };

            generatedRecords.Add(record);
        }

        _context.PayrollRecords.AddRange(generatedRecords);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<List<PayrollRecordDto>>.Success(generatedRecords.Select(p => new PayrollRecordDto
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
            CreatedAt = p.CreatedAt
        }).ToList());
    }
}

public record DisbursePayrollRecordCommand(
    Guid RecordId,
    string PaymentReference
) : IRequest<Result<PayrollRecordDto>>;

public class DisbursePayrollRecordCommandHandler : IRequestHandler<DisbursePayrollRecordCommand, Result<PayrollRecordDto>>
{
    private readonly IClinicDbContext _context;

    public DisbursePayrollRecordCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PayrollRecordDto>> Handle(DisbursePayrollRecordCommand request, CancellationToken cancellationToken)
    {
        var record = await _context.PayrollRecords.FindAsync(new object[] { request.RecordId }, cancellationToken);
        if (record == null)
            return Result<PayrollRecordDto>.Failure("Payroll record not found.", "payroll_not_found");

        record.Status = "Disbursed";
        record.DisbursedAt = DateTime.UtcNow;
        record.PaymentReference = request.PaymentReference;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<PayrollRecordDto>.Success(new PayrollRecordDto
        {
            Id = record.Id,
            PayrollNumber = record.PayrollNumber,
            StaffUserId = record.StaffUserId,
            StaffName = record.StaffName,
            Role = record.Role,
            PeriodMonth = record.PeriodMonth,
            PeriodYear = record.PeriodYear,
            BaseSalary = record.BaseSalary,
            ConsultationsCount = record.ConsultationsCount,
            DoctorCommissionShare = record.DoctorCommissionShare,
            OvertimeHours = record.OvertimeHours,
            OvertimePay = record.OvertimePay,
            TaxDeductions = record.TaxDeductions,
            NetSalaryPayable = record.NetSalaryPayable,
            Status = record.Status,
            DisbursedAt = record.DisbursedAt,
            PaymentReference = record.PaymentReference,
            CreatedAt = record.CreatedAt
        });
    }
}
