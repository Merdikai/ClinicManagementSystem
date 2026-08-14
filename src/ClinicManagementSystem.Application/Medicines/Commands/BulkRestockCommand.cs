using MediatR;

namespace ClinicManagementSystem.Application.Medicines.Commands;

public record BulkRestockCommand(Dictionary<Guid, int> RestockQuantities) : IRequest;
