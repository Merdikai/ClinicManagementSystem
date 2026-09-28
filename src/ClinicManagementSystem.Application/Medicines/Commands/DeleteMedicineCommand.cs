using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
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
        var medicine = await _context.Medicines.FindAsync(new object[] { request.Id }, cancellationToken);
        if (medicine == null)
        {
            return Result<bool>.Failure($"Medicine '{request.Id}' was not found.", "NOT_FOUND");
        }

        _context.Medicines.Remove(medicine);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            await _cache.RemoveByTagAsync("medicines", cancellationToken);
        }
        catch { }

        return Result<bool>.Success(true);
    }
}
