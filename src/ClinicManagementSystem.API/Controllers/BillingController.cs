using Asp.Versioning;
using AutoMapper;
using ClinicManagementSystem.API.Constants;
using ClinicManagementSystem.Application.Billings.Commands;
using ClinicManagementSystem.Application.Billings.Queries;
using ClinicManagementSystem.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/billing")]
[Authorize(Roles = "Admin,Accountant,Patient")]
[EnableRateLimiting(RateLimitingConstants.StaffPolicy)]
public class BillingController : ControllerBase
{
    private readonly ISender _sender;
    public BillingController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("invoices")]
    [Authorize(Roles = "Admin,Accountant")]
    [EndpointSummary("Create a new invoice")]
    [ProducesResponseType(typeof(InvoiceResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceDto dto)
    {
        var command = new CreateInvoiceCommand(
            dto.PatientId,
            dto.AppointmentId,
            dto.TaxAmount,
            dto.DiscountAmount,
            dto.Items
        );

        var invoice = await _sender.Send(command);
        return CreatedAtAction(nameof(GetInvoiceById), new { id = invoice.Id }, invoice);
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

    [HttpGet("invoices/{id:guid}/pdf")]
    [EndpointSummary("Download invoice PDF")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadInvoicePdf(Guid id)
    {
        var (pdfBytes, invoiceNumber) = await _sender.Send(new GetInvoicePdfQuery(id));
        if (pdfBytes is null)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Invoice {id} not found", Status = 404 });

        return File(pdfBytes, "application/pdf", $"Invoice-{invoiceNumber}.pdf");
    }

    [HttpGet("patients/{patientId:guid}/invoices")]
    [EndpointSummary("Get invoices by patient ID")]
    [ProducesResponseType(typeof(IEnumerable<InvoiceResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoicesByPatient(Guid patientId)
    {
        var invoices = await _sender.Send(new GetInvoicesByPatientQuery(patientId));
        return Ok(invoices);
    }

    [HttpGet("invoices/unpaid")]
    [Authorize(Roles = "Admin,Accountant")]
    [EndpointSummary("Get all unpaid invoices")]
    [ProducesResponseType(typeof(IEnumerable<InvoiceResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnpaidInvoices()
    {
        var invoices = await _sender.Send(new GetUnpaidInvoicesQuery());
        return Ok(invoices);
    }

    [HttpPost("payments")]
    [Authorize(Roles = "Admin,Accountant")]
    [EndpointSummary("Process a payment")]
    [ProducesResponseType(typeof(PaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentDto dto)
    {
        var command = new ProcessPaymentCommand(
            dto.InvoiceId,
            dto.AmountPaid,
            dto.PaymentMethod,
            dto.TransactionReference
        );

        var payment = await _sender.Send(command);
        return Ok(payment);
    }
}

