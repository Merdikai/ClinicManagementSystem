using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Prescriptions.Commands;

public record DispensePrescriptionCommand(Guid PrescriptionId) : IRequest<Result<DispenseResponseDto>>;

public class DispensePrescriptionCommandHandler : IRequestHandler<DispensePrescriptionCommand, Result<DispenseResponseDto>>
{
    private readonly IPrescriptionRepository _prescriptionRepository;
    private readonly IMedicineRepository _medicineRepository;

    public DispensePrescriptionCommandHandler(
        IPrescriptionRepository prescriptionRepository,
        IMedicineRepository medicineRepository)
    {
        _prescriptionRepository = prescriptionRepository;
        _medicineRepository = medicineRepository;
    }

    public async Task<Result<DispenseResponseDto>> Handle(DispensePrescriptionCommand request, CancellationToken cancellationToken)
    {
        var prescription = await _prescriptionRepository.GetByIdAsync(request.PrescriptionId);
        if (prescription is null)
            return Result<DispenseResponseDto>.Failure("Prescription not found", "prescription_not_found");

        var dispensedItems = new List<DispensedItemDto>();
        var totalCost = 0m;
        var fullyDispensed = true;

        foreach (var item in prescription.PrescriptionItems)
        {
            var medicine = await _medicineRepository.GetByIdAsync(item.MedicineId);
            if (medicine is null) continue;

            var dispensedQuantity = Math.Min(item.Quantity, medicine.StockQuantity);
            if (dispensedQuantity < item.Quantity)
                fullyDispensed = false;

            if (dispensedQuantity > 0)
            {
                medicine.StockQuantity -= dispensedQuantity;
                _medicineRepository.Update(medicine);
            }

            var itemTotal = dispensedQuantity * item.UnitPrice;
            totalCost += itemTotal;

            dispensedItems.Add(new DispensedItemDto(
                medicine.Name,
                item.Quantity,
                dispensedQuantity,
                item.UnitPrice,
                itemTotal,
                dispensedQuantity == item.Quantity
            ));
        }

        await _medicineRepository.SaveChangesAsync();

        return Result<DispenseResponseDto>.Success(new DispenseResponseDto(
            prescription.Id,
            fullyDispensed,
            dispensedItems,
            totalCost
        ));
    }
}
