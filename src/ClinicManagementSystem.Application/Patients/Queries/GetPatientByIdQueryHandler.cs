using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace ClinicManagementSystem.Application.Patients.Queries;

#pragma warning disable EXTEXP0018
public class GetPatientByIdQueryHandler : IRequestHandler<GetPatientByIdQuery, PatientResponseDto>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;

    public GetPatientByIdQueryHandler(IPatientRepository patientRepository, IMapper mapper, HybridCache cache)
    {
        _patientRepository = patientRepository;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<PatientResponseDto> Handle(GetPatientByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"patient:{request.Id}";

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                var patient = await _patientRepository.GetByIdAsync(request.Id)
                    ?? throw new NotFoundException(nameof(Patient), request.Id);
                return _mapper.Map<PatientResponseDto>(patient);
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
