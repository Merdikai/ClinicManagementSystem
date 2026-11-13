using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Reports.DTOs;
using ClinicManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Reports.Queries;

public class GetDoctorAppointmentCountsQueryHandler : IRequestHandler<GetDoctorAppointmentCountsQuery, IEnumerable<DoctorAppointmentCountDto>>
{
    private readonly IClinicDbContext _context;

    public GetDoctorAppointmentCountsQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DoctorAppointmentCountDto>> Handle(GetDoctorAppointmentCountsQuery request, CancellationToken cancellationToken)
    {
        var doctorsQuery = _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.UserRoles.Any(ur => ur.Role.Name == "Doctor"));

        var filteredAppointments = _context.Appointments
            .AsNoTracking()
            .Where(a => a.ScheduledDateTime >= request.StartDate
                     && a.ScheduledDateTime <= request.EndDate
                     && !a.IsDeleted);

        var rows = await doctorsQuery
            .LeftJoin(
                filteredAppointments,
                doctor => doctor.Id,
                appointment => appointment.DoctorId,
                (doctor, appointment) => new
                {
                    doctor.FirstName,
                    doctor.LastName,
                    AppointmentId = appointment != null ? (Guid?)appointment.Id : null,
                    Status = appointment != null ? (AppointmentStatus?)appointment.Status : null
                })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => $"{r.FirstName} {r.LastName}".Trim())
            .Select(g => new DoctorAppointmentCountDto(
                g.Key,
                g.Count(x => x.AppointmentId.HasValue),
                g.Count(x => x.Status == AppointmentStatus.Completed)
            ))
            .OrderByDescending(d => d.AppointmentCount)
            .ThenBy(d => d.DoctorName)
            .ToList();
    }
}
