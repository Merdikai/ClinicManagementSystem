using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Medicines.Queries;

public class GetMedicinesPagedQueryHandler : IRequestHandler<GetMedicinesPagedQuery, PagedResponse<MedicineResponseDto>>
{
    private readonly IMedicineRepository _medicineRepository;
    private readonly IMapper _mapper;

    public GetMedicinesPagedQueryHandler(IMedicineRepository medicineRepository, IMapper mapper)
    {
        _medicineRepository = medicineRepository;
        _mapper = mapper;
    }

    public async Task<PagedResponse<MedicineResponseDto>> Handle(GetMedicinesPagedQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _medicineRepository.GetPagedAsync(request.Page, request.PageSize, request.Search);
        var dtos = _mapper.Map<IEnumerable<MedicineResponseDto>>(items);
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new PagedResponse<MedicineResponseDto>(
            dtos, totalCount, request.Page, request.PageSize,
            totalPages,
            request.Page < totalPages,
            request.Page > 1
        );
    }
}
