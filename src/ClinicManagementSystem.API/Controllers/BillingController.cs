using ClinicManagementSystem.Application.Billings.Commands;
using ClinicManagementSystem.Application.Billings.Queries;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Asp.Versioning;
using ClinicManagementSystem.API.Constants;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/billing")]
[Tags("Billing")]
[EnableRateLimiting(RateLimitingConstants.StaffPolicy)]
public class BillingController : ControllerBase
{
    private readonly ISender _sender;

    public BillingController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("invoices")]
    [EndpointSummary("Get all invoices")]
    [ProducesResponseType(typeof(IEnumerable<InvoiceResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllInvoices([FromServices] IClinicDbContext context)
    {
        var invoices = await context.Invoices
            .Include(i => i.Patient)
            .Include(i => i.LineItems)
            .Include(i => i.Payments)
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync();

        var result = invoices.Select(i => new InvoiceResponseDto
        {
            Id = i.Id,
            InvoiceNumber = i.InvoiceNumber,
            PatientName = i.Patient != null ? $"{i.Patient.FirstName} {i.Patient.LastName}" : string.Empty,
            IssueDate = i.IssueDate,
            DueDate = i.DueDate,
            Status = i.Status.ToString(),
            SubTotal = i.SubTotal,
            TaxAmount = i.TaxAmount,
            DiscountAmount = i.DiscountAmount,
            TotalAmount = i.TotalAmount,
            BalanceDue = i.BalanceDue,
            LineItems = i.LineItems.Select(li => new InvoiceItemResponseDto
            {
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                LineTotal = li.LineTotal
            }).ToList()
        }).ToList();

        return Ok(result);
    }

    [HttpPost("invoices")]
    [EndpointSummary("Create an invoice")]
    [ProducesResponseType(typeof(InvoiceResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceDto dto)
    {
        var command = new CreateInvoiceCommand(dto.PatientId, dto.AppointmentId, dto.TaxAmount, dto.DiscountAmount, dto.Items);
        var result = await _sender.Send(command);

        return result.Match<IActionResult>(
            onSuccess: invoice => StatusCode(StatusCodes.Status201Created, invoice),
            onFailure: (error, code) => BadRequest(new ProblemDetails { Title = "Invoice Creation Failed", Detail = error, Status = 400 })
        );
    }

    [HttpGet("invoices/{id:guid}")]
    [EndpointSummary("Get invoice by ID")]
    [ProducesResponseType(typeof(InvoiceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvoiceById(Guid id)
    {
        var invoice = await _sender.Send(new GetInvoiceByIdQuery(id));
        if (invoice is null)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Invoice {id} not found", Status = 404 });
        return Ok(invoice);
    }

    [HttpGet("invoices/patient/{patientId:guid}")]
    [EndpointSummary("Get invoices by patient")]
    [ProducesResponseType(typeof(IEnumerable<InvoiceResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoicesByPatient(Guid patientId)
    {
        var invoices = await _sender.Send(new GetInvoicesByPatientQuery(patientId));
        return Ok(invoices);
    }

    [HttpPost("payments")]
    [EndpointSummary("Process a payment against an invoice")]
    [ProducesResponseType(typeof(PaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentDto dto)
    {
        var command = new ProcessPaymentCommand(dto.InvoiceId, dto.AmountPaid, dto.PaymentMethod, dto.TransactionReference);
        var result = await _sender.Send(command);

        return result.Match<IActionResult>(
            onSuccess: payment => Ok(payment),
            onFailure: (error, code) => BadRequest(new ProblemDetails { Title = "Payment Processing Failed", Detail = error, Status = 400 })
        );
    }

    [HttpGet("invoices/{id:guid}/pdf")]
    [EndpointSummary("Download invoice PDF")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPdf(Guid id)
    {
        var (pdfBytes, invoiceNumber) = await _sender.Send(new GetInvoicePdfQuery(id));
        if (pdfBytes is null || pdfBytes.Length == 0)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = "Invoice PDF could not be generated.", Status = 404 });

        return File(pdfBytes, "application/pdf", $"invoice-{invoiceNumber ?? id.ToString()}.pdf");
    }
}
