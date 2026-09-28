using Microsoft.AspNetCore.Authorization;
using Asp.Versioning;
using ClinicManagementSystem.API.Constants;
using ClinicManagementSystem.Application.Laboratories.Commands;
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
[Authorize(Roles = "Admin,Doctor,LabTechnician")]
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

    [HttpPost]
    [EndpointSummary("Create a new diagnostic lab test template")]
    [ProducesResponseType(typeof(LabTestTemplateDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateLabTestTemplateDto dto)
    {
        var result = await _sender.Send(new CreateLabTestTemplateCommand(dto));
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetAll), new { category = result.Value!.Category }, result.Value)
            : BadRequest(result.Error);
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Update an existing diagnostic lab test template")]
    [ProducesResponseType(typeof(LabTestTemplateDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLabTestTemplateDto dto)
    {
        var result = await _sender.Send(new UpdateLabTestTemplateCommand(id, dto));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Delete or deactivate a diagnostic lab test template")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _sender.Send(new DeleteLabTestTemplateCommand(id));
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }
}
