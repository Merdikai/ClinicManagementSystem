using System.Security.Claims;
using Asp.Versioning;
using ClinicManagementSystem.API.Constants;
using ClinicManagementSystem.Application.Extensions;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.API.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/appointments")]
[Tags("Appointments V2")]
[Authorize]
[EnableRateLimiting(RateLimitingConstants.PatientPolicy)]
public class AppointmentsV2Controller : ControllerBase
{
    private readonly IClinicDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public AppointmentsV2Controller(IClinicDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Receptionist,Doctor,Nurse,Patient")]
    [EndpointSummary("V2: Get shaped paginated appointments using SQL column pruning")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] AppointmentStatus? status = null,
        [FromQuery] string? fields = null,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Appointments.AsNoTracking();

        if (User.IsInRole("Patient") && !User.IsInRole("Admin"))
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.FindFirst("email")?.Value
                ?? _currentUserService.Email;

            var patId = string.IsNullOrWhiteSpace(email)
                ? null
                : await _db.Patients.AsNoTracking()
                    .Where(p => p.Email == email)
                    .Select(p => (Guid?)p.Id)
                    .FirstOrDefaultAsync(ct);

            if (!patId.HasValue)
            {
                return Ok(new
                {
                    data = Array.Empty<object>(),
                    meta = new { apiVersion = "2.0", page, pageSize, totalCount = 0, totalPages = 0 }
                });
            }

            query = query.Where(a => a.PatientId == patId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.ScheduledDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .SelectShaped(fields)
            .ToListAsync(ct);

        return Ok(new
        {
            data = items.ShapeData(fields),
            meta = new
            {
                apiVersion = "2.0",
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                shapedFields = string.IsNullOrWhiteSpace(fields) ? "all" : fields,
                timestampUtc = DateTime.UtcNow
            }
        });
    }
}

