using AutoMapper;
using ClinicManagementSystem.Application.Billings.Commands;
using ClinicManagementSystem.Application.Billings.Queries;
using ClinicManagementSystem.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClinicManagementSystem.API.Constants;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[Route("api/v1/billing")]
[Authorize(Roles = "Admin,Accountant")]
[EnableRateLimiting(RateLimitingConstants.StaffPolicy)]
public class BillingController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IMapper _mapper;

    public BillingController(ISender sender, IMapper mapper)
    {
        _sender = sender;
        _mapper = mapper;
    }

    [HttpPost("invoices")]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceDto dto)
    {
        var command = new CreateInvoiceCommand(dto.PatientId, dto.AppointmentId, dto.TaxAmount, dto.DiscountAmount, dto.Items);
        var invoice = await _sender.Send(command);
        return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, invoice);
    }

    [HttpGet("invoices/{id:guid}")]
    public async Task<IActionResult> GetInvoice(Guid id)
    {
        var invoice = await _sender.Send(new GetInvoiceByIdQuery(id));
        return Ok(invoice);
    }

    [HttpGet("patients/{patientId:guid}/invoices")]
    public async Task<IActionResult> GetPatientInvoices(Guid patientId)
    {
        var invoices = await _sender.Send(new GetInvoicesByPatientQuery(patientId));
        return Ok(invoices);
    }

    [HttpPost("payments")]
    public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentDto dto)
    {
        var command = new ProcessPaymentCommand(dto.InvoiceId, dto.AmountPaid, dto.PaymentMethod, dto.TransactionReference);
        var payment = await _sender.Send(command);
        return Ok(payment);
    }
}