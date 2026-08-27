using Asp.Versioning;
using ClinicManagementSystem.API.Constants;
using ClinicManagementSystem.Application.Laboratories.DTOs;
using ClinicManagementSystem.Application.Laboratories.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/lab-tests")]
[Tags("Laboratory Test Catalog")]
[EnableRateLimiting(RateLimitingConstants.StaffPolicy)]
public class LabTestsController : ControllerBase
{
    private readonly ISender _sender;

    public LabTestsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [EndpointSummary("Get all active lab test templates")]
    [ProducesResponseType(typeof(List<LabTestTemplateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? category = null)
    {
        var result = await _sender.Send(new GetLabTestTemplatesQuery(category));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
