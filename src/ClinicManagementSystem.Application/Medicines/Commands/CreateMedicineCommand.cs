using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using MediatR;

namespace ClinicManagementSystem.Application.Medicines.Commands;

public record CreateMedicineCommand(
    string Code,
    string Name,
    string Category,
    int StockQuantity,
    decimal UnitPrice,
    DateTime? ExpiryDate = null,
    string BatchNumber = "",
    string? GenericName = null,
    string? DosageForm = null,
    string? Strength = null,
    string? Manufacturer = null,
    int ReorderLevel = 15,
    decimal CostPrice = 0.0m,
    string? StorageConditions = null,
    bool IsControlledSubstance = false,
    bool IsActive = true
) : IRequest<Result<MedicineResponseDto>>;

