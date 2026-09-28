using System;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Consultations.Queries;

public record GetConsultationByAppointmentQuery(Guid AppointmentId) : IRequest<ConsultationResponseDto?>;

public class GetConsultationByAppointmentQueryHandler : IRequestHandler<GetConsultationByAppointmentQuery, ConsultationResponseDto?>
{
    private readonly IClinicDbContext _context;

    public GetConsultationByAppointmentQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<ConsultationResponseDto?> Handle(GetConsultationByAppointmentQuery request, CancellationToken cancellationToken)
    {
        var c = await _context.Consultations
            .Include(c => c.Doctor)
            .FirstOrDefaultAsync(c => c.AppointmentId == request.AppointmentId, cancellationToken);

        if (c == null) return null;

        return new ConsultationResponseDto
        {
            Id = c.Id,
            AppointmentId = c.AppointmentId,
            DoctorName = c.Doctor != null ? c.Doctor.FirstName + " " + c.Doctor.LastName : "Doctor",
            Symptoms = c.Symptoms,
            Diagnosis = c.Diagnosis,
            ClinicalNotes = c.ClinicalNotes,
            ConsultedAt = c.ConsultedAt
        };
    }
}
