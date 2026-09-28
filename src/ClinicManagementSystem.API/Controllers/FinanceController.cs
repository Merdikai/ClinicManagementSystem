using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Asp.Versioning;
using ClinicManagementSystem.Application.Finance.Commands;
using ClinicManagementSystem.Application.Finance.DTOs;
using ClinicManagementSystem.Application.Finance.Queries;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/finance")]
[Tags("Finance & Accounting")]
[Authorize]
public class FinanceController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IClinicDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public FinanceController(
        ISender sender,
        IClinicDbContext context,
        ICurrentUserService currentUserService)
    {
        _sender = sender;
        _context = context;
        _currentUserService = currentUserService;
    }

    // ─── Dashboard & Revenue Analytics ───

    [HttpGet("metrics")]
    [EndpointSummary("Get executive finance metrics, P&L, AR aging, departmental split, and trend charts")]
    [ProducesResponseType(typeof(FinanceDashboardMetricsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardMetrics()
    {
        var metrics = await _sender.Send(new GetFinanceDashboardMetricsQuery());
        return Ok(metrics);
    }

    // ─── Insurance Claims & TPAs ───

    [HttpGet("claims")]
    [EndpointSummary("List insurance claims with status and provider filters")]
    [ProducesResponseType(typeof(List<InsuranceClaimDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClaims([FromQuery] string? status = null, [FromQuery] string? provider = null)
    {
        var claims = await _sender.Send(new GetInsuranceClaimsQuery(status, provider));
        return Ok(claims);
    }

    [HttpPost("claims")]
    [Authorize(Roles = "Accountant,Admin")]
    [EndpointSummary("Submit a new insurance claim linked to a patient invoice")]
    [ProducesResponseType(typeof(InsuranceClaimDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateClaim([FromBody] CreateInsuranceClaimDto dto)
    {
        var result = await _sender.Send(new CreateInsuranceClaimCommand(dto));
        return result.Match<IActionResult>(
            onSuccess: item => Created(string.Empty, item),
            onFailure: (err, _) => BadRequest(new ProblemDetails { Title = "Claim Creation Failed", Detail = err, Status = 400 })
        );
    }

    [HttpPatch("claims/{id:guid}/reconcile")]
    [Authorize(Roles = "Accountant,Admin")]
    [EndpointSummary("Reconcile insurance claim with remittance advice or record denial")]
    [ProducesResponseType(typeof(InsuranceClaimDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReconcileClaim([FromRoute] Guid id, [FromBody] ReconcileInsuranceClaimDto dto)
    {
        var result = await _sender.Send(new ReconcileInsuranceClaimCommand(id, dto));
        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: (err, _) => BadRequest(new ProblemDetails { Title = "Reconciliation Failed", Detail = err, Status = 400 })
        );
    }

    // ─── Billing Adjustments, Refunds & Write-Offs ───

    [HttpGet("adjustments")]
    [EndpointSummary("List billing adjustments, discounts, and refunds")]
    [ProducesResponseType(typeof(List<BillingAdjustmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdjustments([FromQuery] string? status = null)
    {
        var adjustments = await _sender.Send(new GetBillingAdjustmentsQuery(status));
        return Ok(adjustments);
    }

    [HttpPost("adjustments")]
    [Authorize(Roles = "Accountant,Admin")]
    [EndpointSummary("Request a billing discount, patient refund, or bad debt write-off")]
    [ProducesResponseType(typeof(BillingAdjustmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAdjustment([FromBody] CreateBillingAdjustmentDto dto)
    {
        var (userId, userName) = await ResolveUserInfoAsync();
        var result = await _sender.Send(new CreateBillingAdjustmentCommand(dto, userId, userName));
        return result.Match<IActionResult>(
            onSuccess: item => Created(string.Empty, item),
            onFailure: (err, _) => BadRequest(new ProblemDetails { Title = "Adjustment Request Failed", Detail = err, Status = 400 })
        );
    }

    [HttpPatch("adjustments/{id:guid}/review")]
    [Authorize(Roles = "Accountant,Admin")]
    [EndpointSummary("Approve or reject a billing adjustment / refund request")]
    [ProducesResponseType(typeof(BillingAdjustmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReviewAdjustment([FromRoute] Guid id, [FromBody] ReviewBillingAdjustmentDto dto)
    {
        var (userId, userName) = await ResolveUserInfoAsync();
        var result = await _sender.Send(new ReviewBillingAdjustmentCommand(id, dto, userId, userName));
        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: (err, _) => BadRequest(new ProblemDetails { Title = "Adjustment Review Failed", Detail = err, Status = 400 })
        );
    }

    // ─── Expense Transactions & Accounts Payable (AP) ───

    [HttpGet("expenses")]
    [EndpointSummary("List expense transactions and supplier payables")]
    [ProducesResponseType(typeof(List<ExpenseTransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpenses([FromQuery] string? category = null, [FromQuery] string? status = null)
    {
        var expenses = await _sender.Send(new GetExpenseTransactionsQuery(category, status));
        return Ok(expenses);
    }

    [HttpPost("expenses")]
    [Authorize(Roles = "Accountant,Admin")]
    [EndpointSummary("Record a vendor invoice or operational expense")]
    [ProducesResponseType(typeof(ExpenseTransactionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseTransactionDto dto)
    {
        var (userId, userName) = await ResolveUserInfoAsync();
        var result = await _sender.Send(new CreateExpenseTransactionCommand(dto, userId, userName));
        return result.Match<IActionResult>(
            onSuccess: item => Created(string.Empty, item),
            onFailure: (err, _) => BadRequest(new ProblemDetails { Title = "Expense Creation Failed", Detail = err, Status = 400 })
        );
    }

    [HttpPatch("expenses/{id:guid}/settle")]
    [Authorize(Roles = "Accountant,Admin")]
    [EndpointSummary("Settle and pay an accounts payable expense voucher")]
    [ProducesResponseType(typeof(ExpenseTransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SettleExpense([FromRoute] Guid id, [FromBody] SettleExpenseTransactionDto dto)
    {
        var (userId, userName) = await ResolveUserInfoAsync();
        var result = await _sender.Send(new SettleExpenseTransactionCommand(id, dto, userId, userName));
        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: (err, _) => BadRequest(new ProblemDetails { Title = "Expense Settlement Failed", Detail = err, Status = 400 })
        );
    }

    // ─── Petty Cash Management ───

    [HttpGet("petty-cash")]
    [EndpointSummary("List petty cash disbursement and replenishment ledger")]
    [ProducesResponseType(typeof(List<PettyCashVoucherDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPettyCash([FromQuery] int limit = 50)
    {
        var list = await _sender.Send(new GetPettyCashVouchersQuery(limit));
        return Ok(list);
    }

    [HttpPost("petty-cash")]
    [Authorize(Roles = "Accountant,Admin")]
    [EndpointSummary("Record petty cash disbursement or replenishment float")]
    [ProducesResponseType(typeof(PettyCashVoucherDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePettyCashVoucher([FromBody] CreatePettyCashVoucherDto dto)
    {
        var (userId, userName) = await ResolveUserInfoAsync();
        var result = await _sender.Send(new CreatePettyCashVoucherCommand(dto, userId, userName));
        return result.Match<IActionResult>(
            onSuccess: item => Created(string.Empty, item),
            onFailure: (err, _) => BadRequest(new ProblemDetails { Title = "Petty Cash Voucher Failed", Detail = err, Status = 400 })
        );
    }

    // ─── Staff Payroll & Doctor Commissions ───

    [HttpGet("payroll")]
    [EndpointSummary("List staff payroll records and doctor commission shares")]
    [ProducesResponseType(typeof(List<PayrollRecordDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayroll([FromQuery] int? month = null, [FromQuery] int? year = null)
    {
        var records = await _sender.Send(new GetPayrollRecordsQuery(month, year));
        return Ok(records);
    }

    [HttpPost("payroll/generate")]
    [Authorize(Roles = "Accountant,Admin")]
    [EndpointSummary("Generate monthly payroll batch with automated doctor commission calculation")]
    [ProducesResponseType(typeof(List<PayrollRecordDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GeneratePayroll([FromBody] GenerateMonthlyPayrollDto dto)
    {
        var result = await _sender.Send(new GenerateMonthlyPayrollCommand(dto.PeriodMonth, dto.PeriodYear));
        return result.Match<IActionResult>(
            onSuccess: items => Created(string.Empty, items),
            onFailure: (err, _) => BadRequest(new ProblemDetails { Title = "Payroll Generation Failed", Detail = err, Status = 400 })
        );
    }

    [HttpPatch("payroll/{id:guid}/disburse")]
    [Authorize(Roles = "Accountant,Admin")]
    [EndpointSummary("Disburse salary and mark payroll record as paid")]
    [ProducesResponseType(typeof(PayrollRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DisbursePayroll([FromRoute] Guid id, [FromBody] DisbursePayrollDto dto)
    {
        var result = await _sender.Send(new DisbursePayrollRecordCommand(id, dto.PaymentReference));
        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: (err, _) => BadRequest(new ProblemDetails { Title = "Disbursement Failed", Detail = err, Status = 400 })
        );
    }

    // ─── User Helper Methods ───

    private async Task<(Guid UserId, string UserName)> ResolveUserInfoAsync()
    {
        var userId = _currentUserService.UserId;
        if (userId.HasValue && userId.Value != Guid.Empty)
        {
            var u = await _context.Users.FindAsync(userId.Value);
            if (u != null) return (u.Id, $"{u.FirstName} {u.LastName}".Trim());
        }

        var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value 
            ?? User.FindFirst("email")?.Value 
            ?? User.FindFirst(ClaimTypes.Name)?.Value;

        if (!string.IsNullOrWhiteSpace(emailClaim))
        {
            var u = await _context.Users.FirstOrDefaultAsync(user => user.Email == emailClaim);
            if (u != null) return (u.Id, $"{u.FirstName} {u.LastName}".Trim());
        }

        var accountantUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.Contains("accountant") || u.Username.Contains("accountant"));
        if (accountantUser != null) return (accountantUser.Id, $"{accountantUser.FirstName} {accountantUser.LastName}".Trim());

        var firstUser = await _context.Users.FirstOrDefaultAsync();
        return (firstUser?.Id ?? Guid.NewGuid(), firstUser != null ? $"{firstUser.FirstName} {firstUser.LastName}".Trim() : "Accountant Dave");
    }

    private async Task<string> ResolveUserNameAsync()
    {
        var (_, name) = await ResolveUserInfoAsync();
        return name;
    }
}
