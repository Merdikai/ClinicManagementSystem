using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.DoctorSchedules.Queries;

public record GetDoctorSchedulesByDoctorIdQuery(Guid DoctorId) : IRequest<Result<IEnumerable<DoctorScheduleResponseDto>>>;

public class GetDoctorSchedulesByDoctorIdQueryHandler : IRequestHandler<GetDoctorSchedulesByDoctorIdQuery, Result<IEnumerable<DoctorScheduleResponseDto>>>
{
    private readonly IDoctorScheduleRepository _scheduleRepository;

    public GetDoctorSchedulesByDoctorIdQueryHandler(IDoctorScheduleRepository scheduleRepository)
    {
        _scheduleRepository = scheduleRepository;
    }

    public async Task<Result<IEnumerable<DoctorScheduleResponseDto>>> Handle(GetDoctorSchedulesByDoctorIdQuery request, CancellationToken cancellationToken)
    {
        var schedules = await _scheduleRepository.GetByDoctorIdAsync(request.DoctorId);
        var dtos = schedules.Select(s => new DoctorScheduleResponseDto(
            s.Id,
            s.DoctorId,
            s.Doctor != null ? $"{s.Doctor.FirstName} {s.Doctor.LastName}" : "Unknown",
            s.DayOfWeek,
            s.StartTime,
            s.EndTime,
            s.SlotDurationMinutes,
            s.IsActive
        ));

        return Result<IEnumerable<DoctorScheduleResponseDto>>.Success(dtos);
    }
}
