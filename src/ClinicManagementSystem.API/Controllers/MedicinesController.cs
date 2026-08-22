using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Medicines.Commands;
using ClinicManagementSystem.Application.Medicines.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using ClinicManagementSystem.API.Constants;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/medicines")]
[Tags("Medicines")]
[EnableRateLimiting(RateLimitingConstants.StaffPolicy)]
public class MedicinesController : ControllerBase
{
    private readonly ISender _sender;

    public MedicinesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [EndpointSummary("Get paginated list of medicines")]
    [ProducesResponseType(typeof(PagedResponse<MedicineResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool descending = false)
    {
        var result = await _sender.Send(new GetMedicinesPagedQuery(page, pageSize, search, sortBy, descending));
        return Ok(result);
    }

    [HttpPost]
    [EndpointSummary("Create a new medicine")]
    [ProducesResponseType(typeof(MedicineResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateMedicineDto dto)
    {
        var command = new CreateMedicineCommand(
            dto.Code,
            dto.Name,
            dto.Category,
            dto.StockQuantity,
            dto.UnitPrice,
            dto.ExpiryDate,
            dto.BatchNumber
        );

        var result = await _sender.Send(command);
        return result.Match<IActionResult>(
            onSuccess: medicine => CreatedAtAction(nameof(GetAll), new { id = medicine.Id }, medicine),
            onFailure: (error, code) => BadRequest(new ProblemDetails { Title = "Medicine Creation Failed", Detail = error, Status = 400 })
        );
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Update medicine details")]
    [ProducesResponseType(typeof(MedicineResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMedicine(Guid id, [FromBody] CreateMedicineDto dto, [FromServices] IClinicDbContext context)
    {
        var medicine = await context.Medicines.FindAsync(id);
        if (medicine == null) return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Medicine {id} not found", Status = 404 });

        if (!string.IsNullOrEmpty(dto.Name)) medicine.Name = dto.Name;
        if (!string.IsNullOrEmpty(dto.Category)) medicine.Category = dto.Category;
        medicine.UnitPrice = dto.UnitPrice;
        if (dto.StockQuantity > 0) medicine.StockQuantity = dto.StockQuantity;
        if (dto.ExpiryDate.HasValue) medicine.ExpiryDate = dto.ExpiryDate.Value;
        if (!string.IsNullOrEmpty(dto.BatchNumber)) medicine.BatchNumber = dto.BatchNumber;
        await context.SaveChangesAsync();

        return Ok(new MedicineResponseDto {
            Id = medicine.Id,
            Code = medicine.Code,
            Name = medicine.Name,
            Category = medicine.Category,
            StockQuantity = medicine.StockQuantity,
            UnitPrice = medicine.UnitPrice,
            ExpiryDate = medicine.ExpiryDate,
            BatchNumber = medicine.BatchNumber
        });
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Delete a medicine")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMedicine(Guid id, [FromServices] IClinicDbContext context)
    {
        var medicine = await context.Medicines.FindAsync(id);
        if (medicine == null) return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Medicine {id} not found", Status = 404 });

        context.Medicines.Remove(medicine);
        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:guid}/dispense")]
    [HttpPatch("{id:guid}/dispense")]
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
