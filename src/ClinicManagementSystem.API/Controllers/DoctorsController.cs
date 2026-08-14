using ClinicManagementSystem.Application.Doctors.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[Route("api/v1/doctors")]
[Authorize]
public class DoctorsController : ControllerBase
{
    private readonly ISender _sender;

    public DoctorsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllDoctors()
    {
        var query = new GetDoctorsQuery();
        var doctors = await _sender.Send(query);
        return Ok(doctors);
    }
}