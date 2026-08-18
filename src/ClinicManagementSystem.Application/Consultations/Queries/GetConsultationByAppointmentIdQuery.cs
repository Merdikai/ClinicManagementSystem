using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Consultations.Queries;

public record GetConsultationByAppointmentIdQuery(Guid AppointmentId) : IRequest<ConsultationResponseDto?>;

public class GetConsultationByAppointmentIdQueryHandler : IRequestHandler<GetConsultationByAppointmentIdQuery, ConsultationResponseDto?>
{
    private readonly IClinicDbContext _context;
    private readonly IMapper _mapper;

    public GetConsultationByAppointmentIdQueryHandler(IClinicDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ConsultationResponseDto?> Handle(GetConsultationByAppointmentIdQuery request, CancellationToken cancellationToken)
    {
        var consultation = await _context.Consultations
            .Include(c => c.Appointment!)
                .ThenInclude(a => a.Doctor!)
            .Include(c => c.Prescription!)
                .ThenInclude(p => p.PrescriptionItems!)
                    .ThenInclude(pi => pi.Medicine!)
            .FirstOrDefaultAsync(c => c.AppointmentId == request.AppointmentId, cancellationToken);

        if (consultation is null) return null;

        var dto = _mapper.Map<ConsultationResponseDto>(consultation);
        if (consultation.Appointment?.Doctor is not null)
        {
            dto.DoctorName = $"{consultation.Appointment.Doctor.FirstName} {consultation.Appointment.Doctor.LastName}";
        }
        return dto;
    }
}
