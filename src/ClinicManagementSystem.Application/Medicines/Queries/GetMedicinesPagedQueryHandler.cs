using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace ClinicManagementSystem.Application.Medicines.Queries;

using Microsoft.Extensions.Logging;

#pragma warning disable EXTEXP0018
public class GetMedicinesPagedQueryHandler : IRequestHandler<GetMedicinesPagedQuery, PagedResponse<MedicineResponseDto>>
{
    private readonly IMedicineRepository _medicineRepository;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;
    private readonly ILogger<GetMedicinesPagedQueryHandler> _logger;

    public GetMedicinesPagedQueryHandler(IMedicineRepository medicineRepository, IMapper mapper, HybridCache cache, ILogger<GetMedicinesPagedQueryHandler> logger)
    {
        _medicineRepository = medicineRepository;
        _mapper = mapper;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PagedResponse<MedicineResponseDto>> Handle(GetMedicinesPagedQuery request, CancellationToken cancellationToken)
    {
        if (request.Page == 1 && request.PageSize <= 20 && string.IsNullOrEmpty(request.Search) && string.IsNullOrEmpty(request.SortBy))
        {
            var cacheKey = $"medicines:page{request.Page}:size{request.PageSize}";
            var cacheHit = true;

            var cached = await _cache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    cacheHit = false;
                    _logger.LogInformation("Cache MISS for {CacheKey}", cacheKey);
                    return await GetFromDatabaseAsync(request.Page, request.PageSize, request.Search, request.SortBy, request.Descending, ct);
                },
                options: new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromMinutes(5)
                },
                tags: ["medicines"],
                cancellationToken: cancellationToken
            );

            if (cacheHit)
                _logger.LogInformation("Cache HIT for {CacheKey}", cacheKey);

            return cached;
        }

        return await GetFromDatabaseAsync(request.Page, request.PageSize, request.Search, request.SortBy, request.Descending, cancellationToken);
    }

    private async Task<PagedResponse<MedicineResponseDto>> GetFromDatabaseAsync(int page, int pageSize, string? search, string? sortBy, bool descending, CancellationToken ct)
    {
        var (items, totalCount) = await _medicineRepository.GetPagedAsync(page, pageSize, search, sortBy, descending);
        var dtos = _mapper.Map<IEnumerable<MedicineResponseDto>>(items);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResponse<MedicineResponseDto>(
            dtos, totalCount, page, pageSize,
            totalPages,
            page < totalPages,
            page > 1
        );
    }
}
#pragma warning restore EXTEXP0018
