using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Medicines.Commands;

public class DispenseMedicineCommandHandler : IRequestHandler<DispenseMedicineCommand>
{
    private readonly IMedicineRepository _medicineRepository;

    public DispenseMedicineCommandHandler(IMedicineRepository medicineRepository)
        => _medicineRepository = medicineRepository;

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
    }
}
