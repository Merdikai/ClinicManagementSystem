using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Medicines.Commands;
using ClinicManagementSystem.Application.Medicines.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using ClinicManagementSystem.API.Constants;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/medicines")]
[Authorize(Roles = "Admin,Pharmacist")]
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
    public async Task<IActionResult> Create([FromBody] CreateMedicineDto dto)
    {
        var command = new CreateMedicineCommand(dto.Code, dto.Name, dto.Category, dto.StockQuantity, dto.UnitPrice);
        var medicine = await _sender.Send(command);
        return CreatedAtAction(nameof(Create), new { id = medicine.Id }, medicine);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, [FromQuery] string? sortBy = null, [FromQuery] bool descending = false)
    {
        var medicines = await _sender.Send(new GetMedicinesPagedQuery(page, pageSize, search, sortBy, descending));
        return Ok(medicines);
    }

    [HttpPatch("{id:guid}/dispense")]
    public async Task<IActionResult> Dispense(Guid id, [FromBody] int quantity)
    {
        await _sender.Send(new DispenseMedicineCommand(id, quantity));
        return NoContent();
    }
}