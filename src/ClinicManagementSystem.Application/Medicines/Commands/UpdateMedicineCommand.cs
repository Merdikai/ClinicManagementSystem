using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace ClinicManagementSystem.Application.Medicines.Commands;

public record UpdateMedicineCommand(
    Guid Id,
    string? Code,
    string? Name,
    string? Category,
    decimal UnitPrice,
    int StockQuantity,
    DateTime? ExpiryDate,
    string? BatchNumber,
    string? GenericName = null,
    string? DosageForm = null,
    string? Strength = null,
    string? Manufacturer = null,
    int? ReorderLevel = null,
    decimal? CostPrice = null,
    string? StorageConditions = null,
    bool? IsControlledSubstance = null,
    bool? IsActive = null
) : IRequest<Result<MedicineResponseDto>>;

public class UpdateMedicineCommandHandler : IRequestHandler<UpdateMedicineCommand, Result<MedicineResponseDto>>
{
    private readonly IClinicDbContext _context;
    private readonly HybridCache _cache;

    public UpdateMedicineCommandHandler(IClinicDbContext context, HybridCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<Result<MedicineResponseDto>> Handle(UpdateMedicineCommand request, CancellationToken cancellationToken)
    {
        var medicine = await _context.Medicines.FindAsync(new object[] { request.Id }, cancellationToken);
        if (medicine == null)
        {
            return Result<MedicineResponseDto>.Failure($"Medicine '{request.Id}' was not found.", "NOT_FOUND");
        }

        if (!string.IsNullOrWhiteSpace(request.Name)) medicine.Name = request.Name.Trim();
        if (!string.IsNullOrWhiteSpace(request.Code)) medicine.Code = request.Code.Trim();
        if (!string.IsNullOrWhiteSpace(request.Category)) medicine.Category = request.Category.Trim();
        if (request.GenericName != null) medicine.GenericName = request.GenericName.Trim();
        if (request.DosageForm != null) medicine.DosageForm = request.DosageForm.Trim();
        if (request.Strength != null) medicine.Strength = request.Strength.Trim();
        if (request.Manufacturer != null) medicine.Manufacturer = request.Manufacturer.Trim();
        if (request.UnitPrice > 0) medicine.UnitPrice = request.UnitPrice;
        if (request.CostPrice.HasValue && request.CostPrice.Value >= 0) medicine.CostPrice = request.CostPrice.Value;
        if (request.StockQuantity >= 0) medicine.StockQuantity = request.StockQuantity;
        if (request.ReorderLevel.HasValue && request.ReorderLevel.Value >= 0) medicine.ReorderLevel = request.ReorderLevel.Value;
        if (request.ExpiryDate.HasValue) medicine.ExpiryDate = request.ExpiryDate.Value;
        if (request.BatchNumber != null) medicine.BatchNumber = request.BatchNumber.Trim();
        if (request.StorageConditions != null) medicine.StorageConditions = request.StorageConditions.Trim();
        if (request.IsControlledSubstance.HasValue) medicine.IsControlledSubstance = request.IsControlledSubstance.Value;
        if (request.IsActive.HasValue) medicine.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            await _cache.RemoveByTagAsync("medicines", cancellationToken);
        }
        catch { }

        var dto = new MedicineResponseDto
        {
            Id = medicine.Id,
            Code = medicine.Code,
            Name = medicine.Name,
            GenericName = medicine.GenericName,
            Category = medicine.Category,
            DosageForm = medicine.DosageForm,
            Strength = medicine.Strength,
            Manufacturer = medicine.Manufacturer,
            StockQuantity = medicine.StockQuantity,
            ReorderLevel = medicine.ReorderLevel,
            CostPrice = medicine.CostPrice,
            UnitPrice = medicine.UnitPrice,
            ExpiryDate = medicine.ExpiryDate,
            BatchNumber = medicine.BatchNumber,
            StorageConditions = medicine.StorageConditions,
            IsControlledSubstance = medicine.IsControlledSubstance,
            IsActive = medicine.IsActive
        };

        return Result<MedicineResponseDto>.Success(dto);
    }
}

