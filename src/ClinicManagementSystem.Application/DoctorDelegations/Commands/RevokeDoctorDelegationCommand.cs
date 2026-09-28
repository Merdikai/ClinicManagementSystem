using System;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.DoctorDelegations.Commands;

public record RevokeDoctorDelegationCommand(Guid DelegationId) : IRequest<bool>;

public class RevokeDoctorDelegationCommandHandler : IRequestHandler<RevokeDoctorDelegationCommand, bool>
{
    private readonly IClinicDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public RevokeDoctorDelegationCommandHandler(IClinicDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(RevokeDoctorDelegationCommand request, CancellationToken cancellationToken)
    {
        var delegation = await _context.DoctorDelegations.FindAsync(new object[] { request.DelegationId }, cancellationToken);
        if (delegation == null) return false;

        delegation.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
