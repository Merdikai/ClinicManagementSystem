using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Application.Medicines.Queries;

#pragma warning disable EXTEXP0018
public class GetMedicinesPagedQueryHandler : IRequestHandler<GetMedicinesPagedQuery, PagedResponse<MedicineResponseDto>>
{
    private readonly IMedicineRepository _medicineRepository;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;
    private readonly ILogger<GetMedicinesPagedQueryHandler> _logger;

    public GetMedicinesPagedQueryHandler(
        IMedicineRepository medicineRepository,
        IMapper mapper,
        HybridCache cache,
        ILogger<GetMedicinesPagedQueryHandler> logger)
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
            var cacheKey = $"medicines_v1:page{request.Page}:size{request.PageSize}";
            var state = (_medicineRepository, _mapper, _logger, cacheKey, request);

            return await _cache.GetOrCreateAsync(
                cacheKey,
                state,
                static async (s, ct) =>
                {
                    s._logger.LogDebug("HybridCache miss for {CacheKey}; querying database.", s.cacheKey);
                    return await FetchPagedAsync(s._medicineRepository, s._mapper, s.request, ct);
                },
                options: new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromMinutes(5)
                },
                tags: ["medicines"],
                cancellationToken: cancellationToken
            );
        }

        return await FetchPagedAsync(_medicineRepository, _mapper, request, cancellationToken);
    }

    private static async Task<PagedResponse<MedicineResponseDto>> FetchPagedAsync(
        IMedicineRepository repo,
        IMapper mapper,
        GetMedicinesPagedQuery request,
        CancellationToken ct)
    {
        var (items, totalCount) = await repo.GetPagedAsync(
            request.Page, request.PageSize, request.Search, request.SortBy, request.Descending, ct);
        var dtos = mapper.Map<IEnumerable<MedicineResponseDto>>(items);
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new PagedResponse<MedicineResponseDto>(
            dtos, totalCount, request.Page, request.PageSize,
            totalPages,
            request.Page < totalPages,
            request.Page > 1
        );
    }
}
#pragma warning restore EXTEXP0018
