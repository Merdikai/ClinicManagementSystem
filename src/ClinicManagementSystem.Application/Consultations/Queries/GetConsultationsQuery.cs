using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Consultations.Queries;

public record GetConsultationsQuery(Guid? DoctorId = null, Guid? PatientId = null) : IRequest<List<ConsultationResponseDto>>;

public class GetConsultationsQueryHandler : IRequestHandler<GetConsultationsQuery, List<ConsultationResponseDto>>
{
    private readonly IClinicDbContext _context;

    public GetConsultationsQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<List<ConsultationResponseDto>> Handle(GetConsultationsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Consultations
            .Include(c => c.Doctor)
            .Include(c => c.Appointment)
            .AsQueryable();

        if (request.DoctorId.HasValue)
            query = query.Where(c => c.DoctorId == request.DoctorId.Value);

        if (request.PatientId.HasValue)
            query = query.Where(c => c.Appointment.PatientId == request.PatientId.Value);

        return await query
            .OrderByDescending(c => c.ConsultedAt)
            .Select(c => new ConsultationResponseDto
            {
                Id = c.Id,
                AppointmentId = c.AppointmentId,
                DoctorName = c.Doctor != null ? c.Doctor.FirstName + " " + c.Doctor.LastName : "Doctor",
                Symptoms = c.Symptoms,
                Diagnosis = c.Diagnosis,
                ClinicalNotes = c.ClinicalNotes,
                ConsultedAt = c.ConsultedAt
            })
            .ToListAsync(cancellationToken);
    }
}
