using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace ClinicManagementSystem.Application.Patients.Commands;

public record DeletePatientCommand(Guid Id) : IRequest<Result<bool>>;

public class DeletePatientCommandHandler : IRequestHandler<DeletePatientCommand, Result<bool>>
{
    private readonly IClinicDbContext _context;
    private readonly HybridCache _cache;

    public DeletePatientCommandHandler(IClinicDbContext context, HybridCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<Result<bool>> Handle(DeletePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (patient == null)
        {
            return Result<bool>.Failure($"Patient with ID '{request.Id}' was not found.", "NOT_FOUND");
        }

        patient.IsDeleted = true;
        patient.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            await _cache.RemoveByTagAsync("patients", cancellationToken);
            await _cache.RemoveAsync($"patient_v1:{request.Id}", cancellationToken);
            await _cache.RemoveAsync($"patient_{request.Id}", cancellationToken);
        }
        catch { }

        return Result<bool>.Success(true);
    }
}

public record RestorePatientCommand(Guid Id) : IRequest<Result<bool>>;

public class RestorePatientCommandHandler : IRequestHandler<RestorePatientCommand, Result<bool>>
{
    private readonly IClinicDbContext _context;
    private readonly HybridCache _cache;

    public RestorePatientCommandHandler(IClinicDbContext context, HybridCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<Result<bool>> Handle(RestorePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await _context.Patients
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.IsDeleted, cancellationToken);

        if (patient == null)
        {
            return Result<bool>.Failure($"Deleted patient with ID '{request.Id}' was not found.", "NOT_FOUND");
        }

        patient.IsDeleted = false;
        patient.DeletedAt = null;
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            await _cache.RemoveByTagAsync("patients", cancellationToken);
            await _cache.RemoveAsync($"patient_v1:{request.Id}", cancellationToken);
        }
        catch { }

        return Result<bool>.Success(true);
    }
}

public record GetDeletedPatientsQuery : IRequest<List<PatientResponseDto>>;

public class GetDeletedPatientsQueryHandler : IRequestHandler<GetDeletedPatientsQuery, List<PatientResponseDto>>
{
    private readonly IClinicDbContext _context;
    private readonly AutoMapper.IMapper _mapper;

    public GetDeletedPatientsQueryHandler(IClinicDbContext context, AutoMapper.IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<PatientResponseDto>> Handle(GetDeletedPatientsQuery request, CancellationToken cancellationToken)
    {
        var deleted = await _context.Patients
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => p.IsDeleted)
            .OrderByDescending(p => p.DeletedAt)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<PatientResponseDto>>(deleted);
    }
}
