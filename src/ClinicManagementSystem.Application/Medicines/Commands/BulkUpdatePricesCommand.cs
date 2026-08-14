using MediatR;

namespace ClinicManagementSystem.Application.Medicines.Commands;

public record BulkUpdatePricesCommand(Dictionary<Guid, decimal> PriceUpdates) : IRequest;
