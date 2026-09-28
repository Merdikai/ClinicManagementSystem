using Asp.Versioning;
using ClinicManagementSystem.API.Constants;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Medicines.Commands;
using ClinicManagementSystem.Application.Medicines.Queries;
using MediatR;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

using ClinicManagementSystem.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

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
    private readonly IClinicDbContext _context;

    public MedicinesController(ISender sender, IMapper mapper, IClinicDbContext context)
    {
        _sender = sender;
        _mapper = mapper;
        _context = context;
    }


    [HttpPost]
    [Authorize(Roles = "Admin,Pharmacist")]
    [EndpointSummary("Create a new medicine")]
    [ProducesResponseType(typeof(MedicineResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMedicine([FromBody] CreateMedicineDto dto)
    {
        var command = _mapper.Map<CreateMedicineCommand>(dto);
        var result = await _sender.Send(command);

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
        [FromQuery] bool descending = false)
    {
        var result = await _sender.Send(new GetMedicinesPagedQuery(page, pageSize, search, sortBy, descending));
        return Ok(result);
    }

    [HttpGet("all")]
    [EndpointSummary("Get all active medicines for prescription and POS selection")]
    [ProducesResponseType(typeof(List<MedicineResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllMedicines()
    {
        var medicines = await _context.Medicines
            .Where(m => m.IsActive)
            .OrderBy(m => m.Name)
            .ToListAsync();

        var dtos = _mapper.Map<List<MedicineResponseDto>>(medicines);
        return Ok(dtos);
    }

    [HttpGet("categories")]
    [EndpointSummary("Get list of all formulary medicine categories")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _context.Medicines
            .Where(m => !string.IsNullOrEmpty(m.Category))
            .Select(m => m.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        return Ok(categories);
    }

    [HttpGet("low-stock")]
    [EndpointSummary("Get medicines with low stock levels")]
    [ProducesResponseType(typeof(List<MedicineResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLowStock()
    {
        var medicines = await _context.Medicines
            .Where(m => m.IsActive && m.StockQuantity <= m.ReorderLevel)
            .OrderBy(m => m.StockQuantity)
            .ToListAsync();

        var dtos = _mapper.Map<List<MedicineResponseDto>>(medicines);
        return Ok(dtos);
    }

    [HttpGet("controlled")]
    [EndpointSummary("Get all controlled and restricted formulary substances")]
    [ProducesResponseType(typeof(List<MedicineResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetControlledSubstances()
    {
        var medicines = await _context.Medicines
            .Where(m => m.IsActive && m.IsControlledSubstance)
            .OrderBy(m => m.Name)
            .ToListAsync();

        var dtos = _mapper.Map<List<MedicineResponseDto>>(medicines);
        return Ok(dtos);
    }


    [HttpPost("bulk-restock")]
    [Authorize(Roles = "Admin,Pharmacist")]
    [EndpointSummary("Bulk restock medicine inventory")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> BulkRestock([FromBody] BulkRestockCommand command)
    {
        await _sender.Send(command);
        return NoContent();
    }

    [HttpPost("bulk-update-prices")]
    [Authorize(Roles = "Admin,Pharmacist")]
    [EndpointSummary("Bulk update medicine prices")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> BulkUpdatePrices([FromBody] BulkUpdatePricesCommand command)
    {
        await _sender.Send(command);
        return NoContent();
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Pharmacist")]
    [EndpointSummary("Update medicine details")]
    [ProducesResponseType(typeof(MedicineResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMedicine(Guid id, [FromBody] CreateMedicineDto dto)
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


        var result = await _sender.Send(command);

        return result.Match<IActionResult>(
            onSuccess: medicine => Ok(medicine),
            onFailure: (error, code) => NotFound(new ProblemDetails { Title = "Not Found", Detail = error, Status = 404 })
        );
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Pharmacist")]
    [EndpointSummary("Delete a medicine")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMedicine(Guid id)
    {
        var result = await _sender.Send(new DeleteMedicineCommand(id));

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
    public async Task<IActionResult> Dispense(Guid id, [FromQuery] int? quantity, [FromBody] DispenseRequestDto? dto)
    {
        int qty = dto?.Quantity ?? quantity ?? 1;
        await _sender.Send(new DispenseMedicineCommand(id, qty));
        return NoContent();
    }

    public record DispenseRequestDto(int? Quantity);
}
