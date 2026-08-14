using ClinicManagementSystem.Application.Doctors.Queries;
using ClinicManagementSystem.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClinicManagementSystem.API.Constants;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[Route("api/v1/doctors")]
[Authorize]
[EnableRateLimiting(RateLimitingConstants.StaffPolicy)]
public class DoctorsController : ControllerBase
{
    private readonly ISender _sender;

    public DoctorsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [EndpointSummary("Get all doctors")]
    [ProducesResponseType(typeof(IEnumerable<UserResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllDoctors()
    {
        var query = new GetDoctorsQuery();
        var doctors = await _sender.Send(query);
        return Ok(doctors);
    }
}