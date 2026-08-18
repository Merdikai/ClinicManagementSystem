using Asp.Versioning;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Prescriptions.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/prescriptions")]
[Authorize]
public class PrescriptionsController : ControllerBase
{
    private readonly ISender _sender;

    public PrescriptionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("{prescriptionId:guid}/dispense")]
    [Authorize(Roles = "Pharmacist,Admin")]
    [EndpointSummary("Dispense a prescription and update medicine stock")]
    [ProducesResponseType(typeof(DispenseResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Dispense(Guid prescriptionId)
    {
        var result = await _sender.Send(new DispensePrescriptionCommand(prescriptionId));

        return result.Match<IActionResult>(
            onSuccess: dispense => Ok(dispense),
            onFailure: (error, errorCode) => errorCode switch
            {
                "prescription_not_found" => NotFound(new ProblemDetails { Title = "Not Found", Detail = error, Status = 404 }),
                _ => BadRequest(new ProblemDetails { Title = "Error", Detail = error, Status = 400 })
            }
        );
    }

    [HttpPost("{prescriptionId:guid}/invoice")]
    [Authorize(Roles = "Pharmacist,Accountant,Admin")]
    [EndpointSummary("Generate invoice from a prescription")]
    [ProducesResponseType(typeof(InvoiceResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateInvoice(Guid prescriptionId)
    {
        var result = await _sender.Send(new GenerateInvoiceFromPrescriptionCommand(prescriptionId));

        return result.Match<IActionResult>(
            onSuccess: invoice => CreatedAtAction(nameof(GenerateInvoice), new { prescriptionId }, invoice),
            onFailure: (error, errorCode) => errorCode switch
            {
                "prescription_not_found" => NotFound(new ProblemDetails { Title = "Not Found", Detail = error, Status = 404 }),
                _ => BadRequest(new ProblemDetails { Title = "Error", Detail = error, Status = 400 })
            }
        );
    }
}
