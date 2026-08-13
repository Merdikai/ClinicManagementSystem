using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Patients.Queries;

public class GetPatientsPagedQueryHandler : IRequestHandler<GetPatientsPagedQuery, PagedResponse<PatientResponseDto>>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IMapper _mapper;

    public GetPatientsPagedQueryHandler(IPatientRepository patientRepository, IMapper mapper)
    {
        _patientRepository = patientRepository;
        _mapper = mapper;
    }

    public async Task<PagedResponse<PatientResponseDto>> Handle(GetPatientsPagedQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _patientRepository.GetPagedAsync(request.Page, request.PageSize, request.Search);
        var dtos = _mapper.Map<IEnumerable<PatientResponseDto>>(items);
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new PagedResponse<PatientResponseDto>(
            dtos, totalCount, request.Page, request.PageSize,
            totalPages,
            request.Page < totalPages,
            request.Page > 1
        );
    }
}
