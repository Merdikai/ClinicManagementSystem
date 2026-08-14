using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace ClinicManagementSystem.Application.Medicines.Queries;

#pragma warning disable EXTEXP0018
public class GetMedicinesPagedQueryHandler : IRequestHandler<GetMedicinesPagedQuery, PagedResponse<MedicineResponseDto>>
{
    private readonly IMedicineRepository _medicineRepository;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;

    public GetMedicinesPagedQueryHandler(IMedicineRepository medicineRepository, IMapper mapper, HybridCache cache)
    {
        _medicineRepository = medicineRepository;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<PagedResponse<MedicineResponseDto>> Handle(GetMedicinesPagedQuery request, CancellationToken cancellationToken)
    {
        if (request.Page == 1 && request.PageSize <= 20 && string.IsNullOrEmpty(request.Search))
        {
            var cacheKey = $"medicines:page{request.Page}:size{request.PageSize}";

            return await _cache.GetOrCreateAsync(
                cacheKey,
                async ct => await GetFromDatabaseAsync(request.Page, request.PageSize, request.Search, ct),
                options: new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromMinutes(5)
                },
                tags: ["medicines"],
                cancellationToken: cancellationToken
            );
        }

        return await GetFromDatabaseAsync(request.Page, request.PageSize, request.Search, cancellationToken);
    }

    private async Task<PagedResponse<MedicineResponseDto>> GetFromDatabaseAsync(int page, int pageSize, string? search, CancellationToken ct)
    {
        var (items, totalCount) = await _medicineRepository.GetPagedAsync(page, pageSize, search);
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
