using ClinicManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace ClinicManagementSystem.Application.Medicines.Commands;

#pragma warning disable EXTEXP0018
public class BulkRestockCommandHandler : IRequestHandler<BulkRestockCommand>
{
    private readonly IMedicineRepository _medicineRepository;
    private readonly HybridCache _cache;

    public BulkRestockCommandHandler(IMedicineRepository medicineRepository, HybridCache cache)
    {
        _medicineRepository = medicineRepository;
        _cache = cache;
    }

    public async Task Handle(BulkRestockCommand request, CancellationToken cancellationToken)
    {
        await _medicineRepository.BulkRestockAsync(request.RestockQuantities);
        await _cache.RemoveByTagAsync("medicines", cancellationToken);
    }
}
#pragma warning restore EXTEXP0018
