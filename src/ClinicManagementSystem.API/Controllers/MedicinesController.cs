using Asp.Versioning;
using AutoMapper;
using ClinicManagementSystem.API.Constants;
using ClinicManagementSystem.API.Filters;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Medicines.Commands;
using ClinicManagementSystem.Application.Medicines.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/medicines")]
[Authorize]
[Tags("Medicines")]
[EnableRateLimiting(RateLimitingConstants.StaffPolicy)]
public class MedicinesController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IMapper _mapper;

    public MedicinesController(ISender sender, IMapper mapper)
    {
        _sender = sender;
        _mapper = mapper;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Pharmacist")]
    [EndpointSummary("Create a new medicine")]
    [ProducesResponseType(typeof(MedicineResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMedicine([FromBody] CreateMedicineDto dto, CancellationToken ct = default)
    {
        var command = _mapper.Map<CreateMedicineCommand>(dto);
        var result = await _sender.Send(command, ct);

        return result.Match<IActionResult>(
            onSuccess: medicine => CreatedAtAction(nameof(GetMedicines), new { version = "1.0" }, medicine),
            onFailure: (error, code) => BadRequest(new ProblemDetails { Title = "Medicine Creation Failed", Detail = error, Status = 400 })
        );
    }

    [HttpGet]
    [EndpointSummary("Get paginated list of medicines")]
    [ProducesResponseType(typeof(PagedResponse<MedicineResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMedicines(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool descending = false,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetMedicinesPagedQuery(page, pageSize, search, sortBy, descending), ct);
        return Ok(result);
    }

    [HttpGet("all")]
    [ETagFilter]
    [EndpointSummary("Get all active medicines for prescription and POS selection")]
    [ProducesResponseType(typeof(List<MedicineResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllMedicines(CancellationToken ct = default)
    {
        var dtos = await _sender.Send(new GetAllActiveMedicinesQuery(), ct);
        return Ok(dtos);
    }

    [HttpGet("categories")]
    [EndpointSummary("Get list of all formulary medicine categories")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken ct = default)
    {
        var categories = await _sender.Send(new GetMedicineCategoriesQuery(), ct);
        return Ok(categories);
    }

    [HttpGet("low-stock")]
    [EndpointSummary("Get medicines with low stock levels")]
    [ProducesResponseType(typeof(List<MedicineResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLowStock(CancellationToken ct = default)
    {
        var dtos = await _sender.Send(new GetLowStockMedicinesQuery(), ct);
        return Ok(dtos);
    }

    [HttpGet("controlled")]
    [EndpointSummary("Get all controlled and restricted formulary substances")]
    [ProducesResponseType(typeof(List<MedicineResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetControlledSubstances(CancellationToken ct = default)
    {
        var dtos = await _sender.Send(new GetControlledSubstancesQuery(), ct);
        return Ok(dtos);
    }

    [HttpGet("deleted")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("List soft-deleted medicines for Admin recovery")]
    [ProducesResponseType(typeof(List<MedicineResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeletedMedicines(CancellationToken ct = default)
    {
        var dtos = await _sender.Send(new GetDeletedMedicinesQuery(), ct);
        return Ok(dtos);
    }

    [HttpPost("{id:guid}/restore")]
    [Authorize(Roles = "Admin")]
    [AuditAction("Medicine", "Restored")]
    [EndpointSummary("Restore a soft-deleted medicine (Admin only)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreMedicine(Guid id, CancellationToken ct = default)
    {
        var result = await _sender.Send(new RestoreMedicineCommand(id), ct);
        return result.Match<IActionResult>(
            onSuccess: _ => NoContent(),
            onFailure: (error, _) => NotFound(new ProblemDetails { Title = "Not Found", Detail = error, Status = 404 })
        );
    }

    [HttpPost("bulk-restock")]
    [Authorize(Roles = "Admin,Pharmacist")]
    [EndpointSummary("Bulk restock medicine inventory")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> BulkRestock([FromBody] BulkRestockCommand command, CancellationToken ct = default)
    {
        await _sender.Send(command, ct);
        return NoContent();
    }

    [HttpPost("bulk-update-prices")]
    [Authorize(Roles = "Admin,Pharmacist")]
    [EndpointSummary("Bulk update medicine prices")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> BulkUpdatePrices([FromBody] BulkUpdatePricesCommand command, CancellationToken ct = default)
    {
        await _sender.Send(command, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Pharmacist")]
    [EndpointSummary("Update medicine details")]
    [ProducesResponseType(typeof(MedicineResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMedicine(Guid id, [FromBody] CreateMedicineDto dto, CancellationToken ct = default)
    {
        var command = new UpdateMedicineCommand(
            id,
            dto.Code,
            dto.Name,
            dto.Category,
            dto.UnitPrice,
            dto.StockQuantity,
            dto.ExpiryDate,
            dto.BatchNumber,
            dto.GenericName,
            dto.DosageForm,
            dto.Strength,
            dto.Manufacturer,
            dto.ReorderLevel,
            dto.CostPrice,
            dto.StorageConditions,
            dto.IsControlledSubstance,
            dto.IsActive
        );

        var result = await _sender.Send(command, ct);

        return result.Match<IActionResult>(
            onSuccess: medicine => Ok(medicine),
            onFailure: (error, code) => NotFound(new ProblemDetails { Title = "Not Found", Detail = error, Status = 404 })
        );
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Pharmacist")]
    [AuditAction("Medicine", "Deleted")]
    [EndpointSummary("Soft-delete a medicine")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMedicine(Guid id, CancellationToken ct = default)
    {
        var result = await _sender.Send(new DeleteMedicineCommand(id), ct);

        return result.Match<IActionResult>(
            onSuccess: _ => NoContent(),
            onFailure: (error, code) => NotFound(new ProblemDetails { Title = "Not Found", Detail = error, Status = 404 })
        );
    }

    [HttpPost("{id:guid}/dispense")]
    [HttpPatch("{id:guid}/dispense")]
    [Authorize(Roles = "Admin,Pharmacist")]
    [EndpointSummary("Dispense medicine stock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Dispense(Guid id, [FromQuery] int? quantity, [FromBody] DispenseRequestDto? dto, CancellationToken ct = default)
    {
        int qty = dto?.Quantity ?? quantity ?? 1;
        await _sender.Send(new DispenseMedicineCommand(id, qty), ct);
        return NoContent();
    }

    public record DispenseRequestDto(int? Quantity);
}
