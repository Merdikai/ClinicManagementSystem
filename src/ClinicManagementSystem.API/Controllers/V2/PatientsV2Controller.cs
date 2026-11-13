using Asp.Versioning;
using ClinicManagementSystem.API.Authorization;
using ClinicManagementSystem.API.Constants;
using ClinicManagementSystem.API.Filters;
using ClinicManagementSystem.Application.Extensions;
using ClinicManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.API.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/patients")]
[Tags("Patients V2")]
[EnableRateLimiting(RateLimitingConstants.StaffPolicy)]
[Authorize]
public class PatientsV2Controller : ControllerBase
{
    private readonly IClinicDbContext _db;
    private readonly ILinkGeneratorService _links;
    private readonly IAuthorizationService _authorizationService;

    public PatientsV2Controller(
        IClinicDbContext db,
        ILinkGeneratorService links,
        IAuthorizationService authorizationService)
    {
        _db = db;
        _links = links;
        _authorizationService = authorizationService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist,Pharmacist,LabTechnician,Radiologist,Accountant")]
    [EndpointSummary("V2: Get shaped & paginated patient list using SQL column pruning")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPatients(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? fields = null,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Patients.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p =>
                p.FirstName.ToLower().Contains(term) ||
                p.LastName.ToLower().Contains(term) ||
                p.MedicalRecordNumber.ToLower().Contains(term) ||
                p.Phone.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(ct);

        var entities = await query
            .OrderByDescending(p => p.RegisteredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .SelectShaped(fields)
            .ToListAsync(ct);

        var shapedItems = entities.ShapeData(fields).ToList();

        return Ok(new
        {
            data = shapedItems,
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

    [HttpGet("{id:guid}")]
    [ETagFilter]
    [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist,Pharmacist,LabTechnician,Radiologist,Accountant,Patient")]
    [EndpointSummary("V2: Get shaped patient record with HATEOAS links and ETag support")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPatientById(
        Guid id,
        [FromQuery] string? fields = null,
        CancellationToken ct = default)
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, id, ClinicResourceOperationRequirement.Read);
        if (!authResult.Succeeded)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = "Patients are only authorized to view their own patient file.",
                Status = StatusCodes.Status403Forbidden
            });
        }

        var entity = await _db.Patients
            .AsNoTracking()
            .Where(p => p.Id == id)
            .SelectShaped(fields)
            .FirstOrDefaultAsync(ct);

        if (entity is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = $"Patient '{id}' was not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(new
        {
            data = entity.ShapeSingle(fields),
            links = _links.GeneratePatientLinks(id),
            meta = new
            {
                apiVersion = "2.0",
                shapedFields = string.IsNullOrWhiteSpace(fields) ? "all" : fields,
                timestampUtc = DateTime.UtcNow
            }
        });
    }
}

