using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace ClinicManagementSystem.Application.Medicines.Commands;

#pragma warning disable EXTEXP0018
public class CreateMedicineCommandHandler : IRequestHandler<CreateMedicineCommand, MedicineResponseDto>
{
    private readonly IMedicineRepository _medicineRepository;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;

    public CreateMedicineCommandHandler(IMedicineRepository medicineRepository, IMapper mapper, HybridCache cache)
    {
        _medicineRepository = medicineRepository;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<MedicineResponseDto> Handle(CreateMedicineCommand request, CancellationToken cancellationToken)
    {
        var dto = new CreateMedicineDto(request.Code, request.Name, request.Category, request.StockQuantity, request.UnitPrice);
        var medicine = _mapper.Map<Medicine>(dto);
        await _medicineRepository.AddAsync(medicine);
        await _medicineRepository.SaveChangesAsync();

        await _cache.RemoveByTagAsync("medicines", cancellationToken);

        return _mapper.Map<MedicineResponseDto>(medicine);
    }
}
#pragma warning restore EXTEXP0018
