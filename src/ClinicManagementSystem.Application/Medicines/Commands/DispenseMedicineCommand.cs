using MediatR;

namespace ClinicManagementSystem.Application.Medicines.Commands;

public record DispenseMedicineCommand(Guid MedicineId, int Quantity) : IRequest;
