using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Medicines.Commands;

public class CreateMedicineCommandHandler : IRequestHandler<CreateMedicineCommand, MedicineResponseDto>
{
    private readonly IMedicineRepository _medicineRepository;
    private readonly IMapper _mapper;
    private readonly ILinkGeneratorService _linkGenerator;

    public CreateMedicineCommandHandler(
        IMedicineRepository medicineRepository,
        IMapper mapper,
        ILinkGeneratorService linkGenerator)
    {
        _medicineRepository = medicineRepository;
        _mapper = mapper;
        _linkGenerator = linkGenerator;
    }

    public async Task<MedicineResponseDto> Handle(CreateMedicineCommand request, CancellationToken cancellationToken)
    {
        var medicine = new Medicine
        {
            Code = request.Code,
            Name = request.Name,
            Category = request.Category,
            StockQuantity = request.StockQuantity,
            UnitPrice = request.UnitPrice,
            ExpiryDate = request.ExpiryDate,
            BatchNumber = request.BatchNumber
        };

        await _medicineRepository.AddAsync(medicine);
        var responseDto = _mapper.Map<MedicineResponseDto>(medicine);
        responseDto.Links = _linkGenerator.GenerateMedicineLinks(medicine.Id);
        return responseDto;
    }
}
