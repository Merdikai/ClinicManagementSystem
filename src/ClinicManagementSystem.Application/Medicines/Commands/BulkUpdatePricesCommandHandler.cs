using ClinicManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace ClinicManagementSystem.Application.Medicines.Commands;

#pragma warning disable EXTEXP0018
public class BulkUpdatePricesCommandHandler : IRequestHandler<BulkUpdatePricesCommand>
{
    private readonly IMedicineRepository _medicineRepository;
    private readonly HybridCache _cache;

    public BulkUpdatePricesCommandHandler(IMedicineRepository medicineRepository, HybridCache cache)
    {
        _medicineRepository = medicineRepository;
        _cache = cache;
    }

    public async Task Handle(BulkUpdatePricesCommand request, CancellationToken cancellationToken)
    {
        await _medicineRepository.BulkUpdatePricesAsync(request.PriceUpdates);
        await _cache.RemoveByTagAsync("medicines", cancellationToken);
    }
}
#pragma warning restore EXTEXP0018
