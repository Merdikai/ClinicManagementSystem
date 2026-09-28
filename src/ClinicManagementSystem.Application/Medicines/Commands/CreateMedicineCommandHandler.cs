using AutoMapper;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace ClinicManagementSystem.Application.Medicines.Commands;

#pragma warning disable EXTEXP0018
public class CreateMedicineCommandHandler : IRequestHandler<CreateMedicineCommand, Result<MedicineResponseDto>>
{
    private readonly IMedicineRepository _medicineRepository;
    private readonly IMapper _mapper;
    private readonly ILinkGeneratorService _linkGenerator;
    private readonly HybridCache _cache;

    public CreateMedicineCommandHandler(
        IMedicineRepository medicineRepository,
        IMapper mapper,
        ILinkGeneratorService linkGenerator,
        HybridCache cache)
    {
        _medicineRepository = medicineRepository;
        _mapper = mapper;
        _linkGenerator = linkGenerator;
        _cache = cache;
    }

    public async Task<Result<MedicineResponseDto>> Handle(CreateMedicineCommand request, CancellationToken cancellationToken)
    {
        var medicine = new Medicine
        {
            Code = request.Code,
            Name = request.Name,
            GenericName = request.GenericName ?? string.Empty,
            Category = request.Category,
            DosageForm = request.DosageForm ?? "Tablet",
            Strength = request.Strength ?? string.Empty,
            Manufacturer = request.Manufacturer ?? string.Empty,
            StockQuantity = request.StockQuantity,
            ReorderLevel = request.ReorderLevel,
            CostPrice = request.CostPrice,
            UnitPrice = request.UnitPrice,
            ExpiryDate = request.ExpiryDate,
            BatchNumber = request.BatchNumber ?? string.Empty,
            StorageConditions = request.StorageConditions ?? "Room temperature (15-25°C)",
            IsControlledSubstance = request.IsControlledSubstance,
            IsActive = request.IsActive
        };


        await _medicineRepository.AddAsync(medicine);
        await _medicineRepository.SaveChangesAsync();
        
        // Evict medicines cache so new medicine appears immediately in catalog
        try {
            await _cache.RemoveByTagAsync("medicines", cancellationToken);
        } catch { }

        var responseDto = _mapper.Map<MedicineResponseDto>(medicine);
        responseDto.Links = _linkGenerator.GenerateMedicineLinks(medicine.Id);
        return Result<MedicineResponseDto>.Success(responseDto);
    }
}
#pragma warning restore EXTEXP0018
