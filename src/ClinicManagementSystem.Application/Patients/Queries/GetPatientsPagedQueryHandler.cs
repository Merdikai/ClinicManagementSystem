using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Patients.Queries;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

#pragma warning disable EXTEXP0018
public class GetPatientsPagedQueryHandler : IRequestHandler<GetPatientsPagedQuery, PagedResponse<PatientResponseDto>>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;
    private readonly ILogger<GetPatientsPagedQueryHandler> _logger;

    public GetPatientsPagedQueryHandler(IPatientRepository patientRepository, IMapper mapper, HybridCache cache, ILogger<GetPatientsPagedQueryHandler> logger)
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
            var cacheKey = $"patients:page{request.Page}:size{request.PageSize}";
            var cacheHit = true;

            var cached = await _cache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    cacheHit = false;
                    _logger.LogInformation("Cache MISS for {CacheKey}", cacheKey);
                    return await GetFromDatabaseAsync(request.Page, request.PageSize, request.Search, request.SortBy, request.Descending);
                },
                options: new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromMinutes(5)
                },
                tags: ["patients"],
                cancellationToken: cancellationToken
            );

            if (cacheHit)
                _logger.LogInformation("Cache HIT for {CacheKey}", cacheKey);

            return cached;
        }

        return await GetFromDatabaseAsync(request.Page, request.PageSize, request.Search, request.SortBy, request.Descending);
    }

    private async Task<PagedResponse<PatientResponseDto>> GetFromDatabaseAsync(int page, int pageSize, string? search, string? sortBy, bool descending)
    {
        var (items, totalCount) = await _patientRepository.GetPagedAsync(page, pageSize, search, sortBy, descending);
        var dtos = _mapper.Map<IEnumerable<PatientResponseDto>>(items);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResponse<PatientResponseDto>(
            dtos, totalCount, page, pageSize,
            totalPages,
            page < totalPages,
            page > 1
        );
    }
}
#pragma warning restore EXTEXP0018
