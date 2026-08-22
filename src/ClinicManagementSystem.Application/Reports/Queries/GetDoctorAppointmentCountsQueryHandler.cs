using ClinicManagementSystem.Application.Reports.DTOs;
using ClinicManagementSystem.Application.Interfaces;
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
        var appointments = await _context.Appointments
            .Include(a => a.Doctor)
            .Where(a => a.ScheduledDateTime >= request.StartDate && a.ScheduledDateTime <= request.EndDate && !a.IsDeleted && a.Doctor != null)
            .Select(a => new {
                DoctorName = (a.Doctor.FirstName + " " + a.Doctor.LastName).Trim(),
                IsCompleted = a.Status == Domain.Enums.AppointmentStatus.Completed
            })
            .ToListAsync(cancellationToken);

        return appointments
            .GroupBy(a => a.DoctorName)
            .Select(g => new DoctorAppointmentCountDto(
                g.Key,
                g.Count(),
                g.Count(a => a.IsCompleted)
            ))
            .OrderByDescending(d => d.AppointmentCount)
            .ToList();
    }
}
