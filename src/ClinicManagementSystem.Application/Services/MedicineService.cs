using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;

namespace ClinicManagementSystem.Application.Services;

public class MedicineService : IMedicineService
{
    private readonly IMedicineRepository _medicineRepository;
    private readonly IMapper _mapper;

    public MedicineService(IMedicineRepository medicineRepository, IMapper mapper)
    {
        _medicineRepository = medicineRepository;
        _mapper = mapper;
    }

    public async Task<MedicineResponseDto> CreateAsync(CreateMedicineDto dto)
    {
        var medicine = _mapper.Map<Medicine>(dto);
        await _medicineRepository.AddAsync(medicine);
        await _medicineRepository.SaveChangesAsync();
        return _mapper.Map<MedicineResponseDto>(medicine);
    }

    public async Task<PagedResponse<MedicineResponseDto>> GetPagedAsync(int page, int pageSize, string? search)
    {
        var (items, totalCount) = await _medicineRepository.GetPagedAsync(page, pageSize, search);
        var dtos = _mapper.Map<IEnumerable<MedicineResponseDto>>(items);

        return new PagedResponse<MedicineResponseDto>(
            dtos, totalCount, page, pageSize,
            (int)Math.Ceiling(totalCount / (double)pageSize),
            page < (int)Math.Ceiling(totalCount / (double)pageSize),
            page > 1
        );
    }

    public async Task DispenseAsync(Guid medicineId, int quantity)
    {
        var medicine = await _medicineRepository.GetByIdAsync(medicineId);
        if (medicine is null) throw new NotFoundException(nameof(Medicine), medicineId);
        if (medicine.StockQuantity < quantity)
            throw new BusinessRuleViolationException(
                $"Insufficient stock. Available: {medicine.StockQuantity}, Requested: {quantity}",
                "insufficient_stock");

        medicine.StockQuantity -= quantity;
        _medicineRepository.Update(medicine);
        await _medicineRepository.SaveChangesAsync();
    }
}