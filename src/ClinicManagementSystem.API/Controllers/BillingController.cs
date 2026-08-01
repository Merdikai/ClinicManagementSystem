using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[Route("api/v1/billing")]
[Authorize(Roles = "Admin,Accountant")]
public class BillingController : ControllerBase
{
    private readonly IBillingService _billingService;

    public BillingController(IBillingService billingService)
    {
        _billingService = billingService;
    }

    [HttpPost("invoices")]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceDto dto)
    {
        var invoice = await _billingService.CreateInvoiceAsync(dto);
        return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, invoice);
    }

    [HttpGet("invoices/{id:guid}")]
    public async Task<IActionResult> GetInvoice(Guid id)
    {
        var invoice = await _billingService.GetInvoiceByIdAsync(id);
        return Ok(invoice);
    }

    [HttpGet("patients/{patientId:guid}/invoices")]
    public async Task<IActionResult> GetPatientInvoices(Guid patientId)
    {
        var invoices = await _billingService.GetInvoicesByPatientAsync(patientId);
        return Ok(invoices);
    }

    [HttpPost("payments")]
    public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentDto dto)
    {
        var payment = await _billingService.ProcessPaymentAsync(dto);
        return Ok(payment);
    }
}