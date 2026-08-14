using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace ClinicManagementSystem.Application.Medicines.Commands;

#pragma warning disable EXTEXP0018
public class DispenseMedicineCommandHandler : IRequestHandler<DispenseMedicineCommand>
{
    private readonly IMedicineRepository _medicineRepository;
    private readonly HybridCache _cache;

    public DispenseMedicineCommandHandler(IMedicineRepository medicineRepository, HybridCache cache)
    {
        _medicineRepository = medicineRepository;
        _cache = cache;
    }

    public async Task Handle(DispenseMedicineCommand request, CancellationToken cancellationToken)
    {
        var medicine = await _medicineRepository.GetByIdAsync(request.MedicineId)
            ?? throw new NotFoundException(nameof(Medicine), request.MedicineId);

        if (medicine.StockQuantity < request.Quantity)
            throw new BusinessRuleViolationException(
                $"Insufficient stock. Available: {medicine.StockQuantity}, Requested: {request.Quantity}",
                "insufficient_stock");

        medicine.StockQuantity -= request.Quantity;
        _medicineRepository.Update(medicine);
        await _medicineRepository.SaveChangesAsync();

        await _cache.RemoveByTagAsync("medicines", cancellationToken);
    }
}
#pragma warning restore EXTEXP0018
