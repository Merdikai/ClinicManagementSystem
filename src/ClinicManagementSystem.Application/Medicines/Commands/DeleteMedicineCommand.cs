using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace ClinicManagementSystem.Application.Medicines.Commands;

public record DeleteMedicineCommand(Guid Id) : IRequest<Result<bool>>;

public class DeleteMedicineCommandHandler : IRequestHandler<DeleteMedicineCommand, Result<bool>>
{
    private readonly IClinicDbContext _context;
    private readonly HybridCache _cache;

    public DeleteMedicineCommandHandler(IClinicDbContext context, HybridCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<Result<bool>> Handle(DeleteMedicineCommand request, CancellationToken cancellationToken)
    {
        var medicine = await _context.Medicines.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
        if (medicine == null)
        {
            return Result<bool>.Failure($"Medicine '{request.Id}' was not found.", "NOT_FOUND");
        }

        medicine.IsDeleted = true;
        medicine.DeletedAt = DateTime.UtcNow;
        medicine.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            await _cache.RemoveByTagAsync("medicines", cancellationToken);
        }
        catch { }

        return Result<bool>.Success(true);
    }
}

public record RestoreMedicineCommand(Guid Id) : IRequest<Result<bool>>;

public class RestoreMedicineCommandHandler : IRequestHandler<RestoreMedicineCommand, Result<bool>>
{
    private readonly IClinicDbContext _context;
    private readonly HybridCache _cache;

    public RestoreMedicineCommandHandler(IClinicDbContext context, HybridCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<Result<bool>> Handle(RestoreMedicineCommand request, CancellationToken cancellationToken)
    {
        var medicine = await _context.Medicines
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == request.Id && m.IsDeleted, cancellationToken);

        if (medicine == null)
        {
            return Result<bool>.Failure($"Deleted medicine '{request.Id}' was not found.", "NOT_FOUND");
        }

        medicine.IsDeleted = false;
        medicine.DeletedAt = null;
        medicine.IsActive = true;
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            await _cache.RemoveByTagAsync("medicines", cancellationToken);
        }
        catch { }

        return Result<bool>.Success(true);
    }
}

public record GetDeletedMedicinesQuery : IRequest<List<MedicineResponseDto>>;

public class GetDeletedMedicinesQueryHandler : IRequestHandler<GetDeletedMedicinesQuery, List<MedicineResponseDto>>
{
    private readonly IClinicDbContext _context;
    private readonly AutoMapper.IMapper _mapper;

    public GetDeletedMedicinesQueryHandler(IClinicDbContext context, AutoMapper.IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<MedicineResponseDto>> Handle(GetDeletedMedicinesQuery request, CancellationToken cancellationToken)
    {
        var deleted = await _context.Medicines
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(m => m.IsDeleted)
            .OrderByDescending(m => m.DeletedAt)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<MedicineResponseDto>>(deleted);
    }
}
