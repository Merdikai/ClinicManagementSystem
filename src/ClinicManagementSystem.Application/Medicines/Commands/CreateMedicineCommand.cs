using ClinicManagementSystem.Application.DTOs;
using MediatR;

namespace ClinicManagementSystem.Application.Medicines.Commands;

public record CreateMedicineCommand(
    string Code,
    string Name,
    string Category,
    int StockQuantity,
    decimal UnitPrice
) : IRequest<MedicineResponseDto>;
