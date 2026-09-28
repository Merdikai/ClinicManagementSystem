using System;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Consultations.Queries;

public record GetConsultationByIdQuery(Guid Id) : IRequest<ConsultationResponseDto?>;

public class GetConsultationByIdQueryHandler : IRequestHandler<GetConsultationByIdQuery, ConsultationResponseDto?>
{
    private readonly IClinicDbContext _context;

    public GetConsultationByIdQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<ConsultationResponseDto?> Handle(GetConsultationByIdQuery request, CancellationToken cancellationToken)
    {
        var c = await _context.Consultations
            .Include(c => c.Doctor)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

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
