using Asp.Versioning;
using AutoMapper;
using ClinicManagementSystem.API.Authorization;
using ClinicManagementSystem.API.Constants;
using ClinicManagementSystem.Application.Billings.Commands;
using ClinicManagementSystem.Application.Billings.Queries;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/billing")]
[Tags("Billing")]
[Authorize]
[EnableRateLimiting(RateLimitingConstants.StaffPolicy)]
public class BillingController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IMapper _mapper;
    private readonly IClinicDbContext _context;
    private readonly IAuthorizationService _authorizationService;

    public BillingController(
        ISender sender,
        IMapper mapper,
        IClinicDbContext context,
        IAuthorizationService authorizationService)
    {
        _sender = sender;
        _mapper = mapper;
        _context = context;
        _authorizationService = authorizationService;
    }

    private async Task<Guid?> GetCurrentPatientIdAsync(CancellationToken ct = default)
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("email")?.Value;

        if (string.IsNullOrWhiteSpace(email))
            return null;

        var pat = await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Email == email, ct);

        return pat?.Id;
    }

    [HttpGet("invoices")]
    [Authorize(Roles = "Admin,Accountant,Receptionist,Pharmacist,LabTechnician,Doctor,Patient")]
    [EndpointSummary("Get all invoices or filter by patient")]
    [ProducesResponseType(typeof(IEnumerable<InvoiceResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllInvoices([FromQuery] Guid? patientId = null, [FromQuery] string? department = null, CancellationToken ct = default)
    {
        if (User.IsInRole("Patient") && !User.IsInRole("Admin"))
        {
            var myPatientId = await GetCurrentPatientIdAsync(ct);
            if (!myPatientId.HasValue) return Ok(new List<InvoiceResponseDto>());
            patientId = myPatientId.Value;
        }

        bool isPharmacist = User.IsInRole("Pharmacist");
        bool isLabTech = User.IsInRole("LabTechnician");
        var invoices = await _sender.Send(new GetAllInvoicesQuery(
            patientId,
            Department: isPharmacist ? "Pharmacy" : (isLabTech ? "Laboratory" : department),
            PharmacyOnly: isPharmacist,
            LaboratoryOnly: isLabTech
        ), ct);
        return Ok(invoices);
    }

    [HttpPost("invoices")]
    [Authorize(Roles = "Accountant,Receptionist,Admin")]
    [EndpointSummary("Create an invoice")]
    [ProducesResponseType(typeof(InvoiceResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceDto dto, CancellationToken ct)
    {
        var command = new CreateInvoiceCommand(
            dto.PatientId,
            dto.AppointmentId,
            dto.TaxAmount,
            dto.DiscountAmount,
            dto.Items ?? new List<CreateInvoiceItemDto>()
        );
        var result = await _sender.Send(command, ct);

        return result.Match<IActionResult>(
            onSuccess: invoice => CreatedAtAction(nameof(GetInvoiceById), new { id = invoice.Id }, invoice),
            onFailure: (error, errorCode) => BadRequest(new ProblemDetails
            {
                Title = "Error",
                Detail = error,
                Status = 400
            })
        );
    }

    [HttpGet("invoices/{id:guid}")]
    [Authorize(Roles = "Admin,Accountant,Receptionist,Pharmacist,LabTechnician,Doctor,Patient")]
    [EndpointSummary("Get invoice by ID")]
    [ProducesResponseType(typeof(InvoiceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetInvoiceById(Guid id, CancellationToken ct)
    {
        var invoice = await _sender.Send(new GetInvoiceByIdQuery(id), ct);
        if (invoice == null)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = "Invoice not found", Status = 404 });

        var authResult = await _authorizationService.AuthorizeAsync(User, invoice, ClinicResourceOperationRequirement.Read);
        if (!authResult.Succeeded)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = "You are not authorized to view this invoice.",
                Status = StatusCodes.Status403Forbidden
            });
        }

        return Ok(invoice);
    }

    [HttpGet("invoices/{id:guid}/pdf")]
    [Authorize(Roles = "Admin,Accountant,Receptionist,Pharmacist,LabTechnician,Doctor,Patient")]
    [EndpointSummary("Download invoice PDF in customizable design theme")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetInvoicePdf(Guid id, [FromQuery] string? design = "ExecutiveNavy", CancellationToken ct = default)
    {
        var invoice = await _sender.Send(new GetInvoiceByIdQuery(id), ct);
        if (invoice is null)
            return NotFound(new ProblemDetails { Title = "Invoice Not Found", Detail = $"Invoice with ID '{id}' was not found.", Status = 404 });

        var authResult = await _authorizationService.AuthorizeAsync(User, invoice, ClinicResourceOperationRequirement.Read);
        if (!authResult.Succeeded)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = "You are not authorized to download this invoice receipt.",
                Status = StatusCodes.Status403Forbidden
            });
        }

        var (pdfBytes, invoiceNumber) = await _sender.Send(new GetInvoicePdfQuery(id, design), ct);
        if (pdfBytes is null)
            return NotFound(new ProblemDetails { Title = "Invoice Not Found", Detail = $"Invoice with ID '{id}' was not found.", Status = 404 });

        var themeSuffix = string.IsNullOrWhiteSpace(design) ? "" : $"-{design.ToLower()}";
        return File(pdfBytes, "application/pdf", $"Invoice-{invoiceNumber ?? id.ToString()}{themeSuffix}.pdf");
    }

    [HttpPost("invoices/{id:guid}/payments")]
    [Authorize(Roles = "Accountant,Receptionist,Admin")]
    [EndpointSummary("Process payment for an invoice")]
    [ProducesResponseType(typeof(PaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessPayment(Guid id, [FromBody] ProcessPaymentDto dto, CancellationToken ct)
    {
        var invoiceId = id != Guid.Empty ? id : dto.InvoiceId;
        var command = new ProcessPaymentCommand(invoiceId, dto.AmountPaid, dto.PaymentMethod ?? "Cash", dto.TransactionReference);
        var result = await _sender.Send(command, ct);

        return result.Match<IActionResult>(
            onSuccess: payment => Ok(payment),
            onFailure: (error, errorCode) => BadRequest(new ProblemDetails
            {
                Title = "Error",
                Detail = error,
                Status = 400
            })
        );
    }

    [HttpPost("payments")]
    [Authorize(Roles = "Accountant,Receptionist,Admin")]
    [EndpointSummary("Process payment with invoice ID in request body")]
    [ProducesResponseType(typeof(PaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessPaymentDirect([FromBody] ProcessPaymentDto dto, CancellationToken ct)
    {
        var command = new ProcessPaymentCommand(dto.InvoiceId, dto.AmountPaid, dto.PaymentMethod ?? "Cash", dto.TransactionReference);
        var result = await _sender.Send(command, ct);

        return result.Match<IActionResult>(
            onSuccess: payment => Ok(payment),
            onFailure: (error, errorCode) => BadRequest(new ProblemDetails
            {
                Title = "Error",
                Detail = error,
                Status = 400
            })
        );
    }
}
