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
        return await _context.Appointments
            .Include(a => a.Doctor)
            .Where(a => a.ScheduledDateTime >= request.StartDate && a.ScheduledDateTime <= request.EndDate && !a.IsDeleted)
            .GroupBy(a => new { a.Doctor.FirstName, a.Doctor.LastName })
            .Select(g => new DoctorAppointmentCountDto(
                $"{g.Key.FirstName} {g.Key.LastName}",
                g.Count(),
                g.Count(a => a.Status == Domain.Enums.AppointmentStatus.Completed)
            ))
            .OrderByDescending(d => d.AppointmentCount)
            .ToListAsync(cancellationToken);
    }
}
