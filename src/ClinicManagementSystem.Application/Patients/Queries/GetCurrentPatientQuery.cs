using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Patients.Queries;

public record GetCurrentPatientQuery(string? Email, string? Username) : IRequest<PatientResponseDto?>;

public class GetCurrentPatientQueryHandler : IRequestHandler<GetCurrentPatientQuery, PatientResponseDto?>
{
    private readonly IClinicDbContext _context;
    private readonly IMapper _mapper;

    public GetCurrentPatientQueryHandler(IClinicDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PatientResponseDto?> Handle(GetCurrentPatientQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Username))
            return null;

        var patient = await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p =>
                (!string.IsNullOrEmpty(request.Email) && p.Email == request.Email) ||
                (!string.IsNullOrEmpty(request.Username) && p.Email == request.Username),
                cancellationToken);

        if (patient is null)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    (!string.IsNullOrEmpty(request.Email) && u.Email == request.Email) ||
                    (!string.IsNullOrEmpty(request.Username) && (u.Username == request.Username || u.Email == request.Username)),
                    cancellationToken);

            if (user != null)
            {
                patient = new Patient
                {
                    Id = Guid.NewGuid(),
                    FirstName = !string.IsNullOrWhiteSpace(user.FirstName) ? user.FirstName : user.Username,
                    LastName = !string.IsNullOrWhiteSpace(user.LastName) ? user.LastName : "Patient",
                    Email = user.Email,
                    Phone = !string.IsNullOrWhiteSpace(user.PhoneNumber) ? user.PhoneNumber : "+15550000000",
                    DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Gender = "Other",
                    Address = "Registered Clinic Patient",
                    MedicalRecordNumber = $"MRN-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
                    RegisteredAt = DateTime.UtcNow
                };
                _context.Patients.Add(patient);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        return patient is null ? null : _mapper.Map<PatientResponseDto>(patient);
    }
}

public record GetDoctorPanelQuery(Guid DoctorId) : IRequest<List<PatientResponseDto>>;

public class GetDoctorPanelQueryHandler : IRequestHandler<GetDoctorPanelQuery, List<PatientResponseDto>>
{
    private readonly IClinicDbContext _context;
    private readonly IMapper _mapper;

    public GetDoctorPanelQueryHandler(IClinicDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<PatientResponseDto>> Handle(GetDoctorPanelQuery request, CancellationToken cancellationToken)
    {
        if (request.DoctorId == Guid.Empty)
            return new List<PatientResponseDto>();

        var patientIdsFromAppts = await _context.Appointments
            .AsNoTracking()
            .Where(a => a.DoctorId == request.DoctorId)
            .Select(a => a.PatientId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var patientIdsFromConsults = await _context.Consultations
            .AsNoTracking()
            .Where(c => c.DoctorId == request.DoctorId)
            .Join(_context.Appointments.AsNoTracking(), c => c.AppointmentId, a => a.Id, (c, a) => a.PatientId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var allAssignedPatientIds = patientIdsFromAppts.Union(patientIdsFromConsults).Distinct().ToList();

        var patients = await _context.Patients
            .AsNoTracking()
            .Where(p => allAssignedPatientIds.Contains(p.Id))
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<PatientResponseDto>>(patients);
    }
}

