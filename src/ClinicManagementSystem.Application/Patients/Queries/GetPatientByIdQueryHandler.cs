using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace ClinicManagementSystem.Application.Patients.Queries;

using ClinicManagementSystem.Application.Interfaces;

#pragma warning disable EXTEXP0018
public class GetPatientByIdQueryHandler : IRequestHandler<GetPatientByIdQuery, PatientResponseDto>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;
    private readonly ILinkGeneratorService _linkGenerator;

    public GetPatientByIdQueryHandler(IPatientRepository patientRepository, IMapper mapper, HybridCache cache, ILinkGeneratorService linkGenerator)
    {
        _patientRepository = patientRepository;
        _mapper = mapper;
        _cache = cache;
        _linkGenerator = linkGenerator;
    }

    public async Task<PatientResponseDto> Handle(GetPatientByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"patient_v1:{request.Id}";

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                var patient = await _patientRepository.GetByIdAsync(request.Id)
                    ?? throw new NotFoundException(nameof(Patient), request.Id);
                var dto = _mapper.Map<PatientResponseDto>(patient);
                dto.Links = _linkGenerator.GeneratePatientLinks(patient.Id);
                return dto;
            },
            options: new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(10)
            },
            tags: ["patients"],
            cancellationToken: cancellationToken
        );
    }
}
#pragma warning restore EXTEXP0018
