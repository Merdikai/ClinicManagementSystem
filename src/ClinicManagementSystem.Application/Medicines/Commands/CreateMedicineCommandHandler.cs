using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Medicines.Commands;

public class CreateMedicineCommandHandler : IRequestHandler<CreateMedicineCommand, MedicineResponseDto>
{
    private readonly IMedicineRepository _medicineRepository;
    private readonly IMapper _mapper;

    public CreateMedicineCommandHandler(IMedicineRepository medicineRepository, IMapper mapper)
    {
        _medicineRepository = medicineRepository;
        _mapper = mapper;
    }

    public async Task<MedicineResponseDto> Handle(CreateMedicineCommand request, CancellationToken cancellationToken)
    {
        var dto = new CreateMedicineDto(request.Code, request.Name, request.Category, request.StockQuantity, request.UnitPrice);
        var medicine = _mapper.Map<Medicine>(dto);
        await _medicineRepository.AddAsync(medicine);
        await _medicineRepository.SaveChangesAsync();
        return _mapper.Map<MedicineResponseDto>(medicine);
    }
}
