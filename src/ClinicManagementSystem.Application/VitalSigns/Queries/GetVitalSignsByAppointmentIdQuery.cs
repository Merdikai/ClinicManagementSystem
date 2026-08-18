using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.VitalSigns.Queries;

public record GetVitalSignsByAppointmentIdQuery(Guid AppointmentId) : IRequest<VitalSignResponseDto?>;

public class GetVitalSignsByAppointmentIdQueryHandler : IRequestHandler<GetVitalSignsByAppointmentIdQuery, VitalSignResponseDto?>
{
    private readonly IClinicDbContext _context;
    private readonly IMapper _mapper;

    public GetVitalSignsByAppointmentIdQueryHandler(IClinicDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<VitalSignResponseDto?> Handle(GetVitalSignsByAppointmentIdQuery request, CancellationToken cancellationToken)
    {
        var vitals = await _context.VitalSigns
            .FirstOrDefaultAsync(v => v.AppointmentId == request.AppointmentId, cancellationToken);

        return vitals is null ? null : _mapper.Map<VitalSignResponseDto>(vitals);
    }
}
