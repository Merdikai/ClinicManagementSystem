using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[Route("api/v1/medicines")]
public class MedicinesController : ControllerBase
{
    private readonly IMedicineService _medicineService;

    public MedicinesController(IMedicineService medicineService)
    {
        _medicineService = medicineService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMedicineDto dto)
    {
        var medicine = await _medicineService.CreateAsync(dto);
        return CreatedAtAction(nameof(Create), new { id = medicine.Id }, medicine);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var medicines = await _medicineService.GetPagedAsync(page, pageSize, search);
        return Ok(medicines);
    }

    [HttpPatch("{id:guid}/dispense")]
    public async Task<IActionResult> Dispense(Guid id, [FromBody] int quantity)
    {
        await _medicineService.DispenseAsync(id, quantity);
        return NoContent();
    }
}