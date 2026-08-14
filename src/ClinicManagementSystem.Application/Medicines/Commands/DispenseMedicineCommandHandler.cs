using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

using ClinicManagementSystem.Application.Interfaces;

namespace ClinicManagementSystem.Application.Medicines.Commands;

#pragma warning disable EXTEXP0018
public class DispenseMedicineCommandHandler : IRequestHandler<DispenseMedicineCommand>
{
    private readonly IMedicineRepository _medicineRepository;
    private readonly HybridCache _cache;
    private readonly INotificationService _notificationService;

    public DispenseMedicineCommandHandler(IMedicineRepository medicineRepository, HybridCache cache, INotificationService notificationService)
    {
        _medicineRepository = medicineRepository;
        _cache = cache;
        _notificationService = notificationService;
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

        if (medicine.StockQuantity < 10)
        {
            await _notificationService.NotifyLowStockAsync(medicine.Name, medicine.StockQuantity);
        }

        await _cache.RemoveByTagAsync("medicines", cancellationToken);
    }
}
#pragma warning restore EXTEXP0018
