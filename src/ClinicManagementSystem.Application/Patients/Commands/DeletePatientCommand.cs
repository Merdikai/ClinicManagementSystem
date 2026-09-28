using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
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
        var patient = await _context.Patients.FindAsync(new object[] { request.Id }, cancellationToken);
        if (patient == null)
        {
            return Result<bool>.Failure($"Patient with ID '{request.Id}' was not found.", "NOT_FOUND");
        }

        _context.Patients.Remove(patient);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            await _cache.RemoveByTagAsync("patients", cancellationToken);
            await _cache.RemoveAsync($"patient_{request.Id}", cancellationToken);
        }
        catch { }

        return Result<bool>.Success(true);
    }
}
