using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Application.Patients.Queries;

#pragma warning disable EXTEXP0018
public class GetPatientsPagedQueryHandler : IRequestHandler<GetPatientsPagedQuery, PagedResponse<PatientResponseDto>>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;
    private readonly ILogger<GetPatientsPagedQueryHandler> _logger;

    public GetPatientsPagedQueryHandler(
        IPatientRepository patientRepository,
        IMapper mapper,
        HybridCache cache,
        ILogger<GetPatientsPagedQueryHandler> logger)
    {
        _patientRepository = patientRepository;
        _mapper = mapper;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PagedResponse<PatientResponseDto>> Handle(GetPatientsPagedQuery request, CancellationToken cancellationToken)
    {
        if (request.Page == 1 && request.PageSize <= 20 && string.IsNullOrEmpty(request.Search) && string.IsNullOrEmpty(request.SortBy))
        {
            var cacheKey = $"patients_v1:page{request.Page}:size{request.PageSize}";
            var state = (_patientRepository, _mapper, _logger, cacheKey, request);

            return await _cache.GetOrCreateAsync(
                cacheKey,
                state,
                static async (s, ct) =>
                {
                    s._logger.LogDebug("HybridCache miss for {CacheKey}; querying database.", s.cacheKey);
                    return await FetchPagedAsync(s._patientRepository, s._mapper, s.request, ct);
                },
                options: new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromMinutes(5)
                },
                tags: ["patients"],
                cancellationToken: cancellationToken
            );
        }

        return await FetchPagedAsync(_patientRepository, _mapper, request, cancellationToken);
    }

    private static async Task<PagedResponse<PatientResponseDto>> FetchPagedAsync(
        IPatientRepository repo,
        IMapper mapper,
        GetPatientsPagedQuery request,
        CancellationToken ct)
    {
        var (items, totalCount) = await repo.GetPagedAsync(
            request.Page, request.PageSize, request.Search, request.SortBy, request.Descending, ct);
        var dtos = mapper.Map<IEnumerable<PatientResponseDto>>(items);
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new PagedResponse<PatientResponseDto>(
            dtos, totalCount, request.Page, request.PageSize,
            totalPages,
            request.Page < totalPages,
            request.Page > 1
        );
    }
}
#pragma warning restore EXTEXP0018
