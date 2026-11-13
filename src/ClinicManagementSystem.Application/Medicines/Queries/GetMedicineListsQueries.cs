using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Medicines.Queries;

public record GetAllActiveMedicinesQuery : IRequest<List<MedicineResponseDto>>;
public record GetMedicineCategoriesQuery : IRequest<List<string>>;
public record GetLowStockMedicinesQuery : IRequest<List<MedicineResponseDto>>;
public record GetControlledSubstancesQuery : IRequest<List<MedicineResponseDto>>;

public class GetAllActiveMedicinesQueryHandler : IRequestHandler<GetAllActiveMedicinesQuery, List<MedicineResponseDto>>
{
    private readonly IClinicDbContext _context;
    private readonly IMapper _mapper;

    public GetAllActiveMedicinesQueryHandler(IClinicDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<MedicineResponseDto>> Handle(GetAllActiveMedicinesQuery request, CancellationToken cancellationToken)
    {
        var medicines = await _context.Medicines
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<MedicineResponseDto>>(medicines);
    }
}

public class GetMedicineCategoriesQueryHandler : IRequestHandler<GetMedicineCategoriesQuery, List<string>>
{
    private readonly IClinicDbContext _context;

    public GetMedicineCategoriesQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<List<string>> Handle(GetMedicineCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Medicines
            .AsNoTracking()
            .Where(m => !string.IsNullOrEmpty(m.Category))
            .Select(m => m.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }
}

public class GetLowStockMedicinesQueryHandler : IRequestHandler<GetLowStockMedicinesQuery, List<MedicineResponseDto>>
{
    private readonly IClinicDbContext _context;
    private readonly IMapper _mapper;

    public GetLowStockMedicinesQueryHandler(IClinicDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<MedicineResponseDto>> Handle(GetLowStockMedicinesQuery request, CancellationToken cancellationToken)
    {
        var medicines = await _context.Medicines
            .AsNoTracking()
            .Where(m => m.IsActive && m.StockQuantity <= m.ReorderLevel)
            .OrderBy(m => m.StockQuantity)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<MedicineResponseDto>>(medicines);
    }
}

public class GetControlledSubstancesQueryHandler : IRequestHandler<GetControlledSubstancesQuery, List<MedicineResponseDto>>
{
    private readonly IClinicDbContext _context;
    private readonly IMapper _mapper;

    public GetControlledSubstancesQueryHandler(IClinicDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<MedicineResponseDto>> Handle(GetControlledSubstancesQuery request, CancellationToken cancellationToken)
    {
        var medicines = await _context.Medicines
            .AsNoTracking()
            .Where(m => m.IsActive && m.IsControlledSubstance)
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<MedicineResponseDto>>(medicines);
    }
}

