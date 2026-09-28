using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.DoctorSchedules.Queries;

public record GetAllDoctorSchedulesQuery() : IRequest<Result<IEnumerable<DoctorScheduleResponseDto>>>;

public class GetAllDoctorSchedulesQueryHandler : IRequestHandler<GetAllDoctorSchedulesQuery, Result<IEnumerable<DoctorScheduleResponseDto>>>
{
    private readonly IDoctorScheduleRepository _scheduleRepository;

    public GetAllDoctorSchedulesQueryHandler(IDoctorScheduleRepository scheduleRepository)
    {
        _scheduleRepository = scheduleRepository;
    }

    public async Task<Result<IEnumerable<DoctorScheduleResponseDto>>> Handle(GetAllDoctorSchedulesQuery request, CancellationToken cancellationToken)
    {
        var schedules = await _scheduleRepository.GetAllAsync();
        var dtos = schedules.Select(s => new DoctorScheduleResponseDto(
            s.Id,
            s.DoctorId,
            s.Doctor != null ? $"{s.Doctor.FirstName} {s.Doctor.LastName}" : "Unknown Doctor",
            s.DayOfWeek,
            s.StartTime,
            s.EndTime,
            s.SlotDurationMinutes,
            s.IsActive
        ));

        return Result<IEnumerable<DoctorScheduleResponseDto>>.Success(dtos);
    }
}
