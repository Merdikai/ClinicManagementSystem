using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.DoctorSchedules.Commands;

public record CreateDoctorScheduleCommand(
    Guid DoctorId,
    DayOfWeek DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime,
    int SlotDurationMinutes
) : IRequest<Result<DoctorScheduleResponseDto>>;

public class CreateDoctorScheduleCommandHandler : IRequestHandler<CreateDoctorScheduleCommand, Result<DoctorScheduleResponseDto>>
{
    private readonly IDoctorScheduleRepository _scheduleRepository;
    private readonly IUserRepository _userRepository;

    public CreateDoctorScheduleCommandHandler(
        IDoctorScheduleRepository scheduleRepository,
        IUserRepository userRepository)
    {
        _scheduleRepository = scheduleRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<DoctorScheduleResponseDto>> Handle(CreateDoctorScheduleCommand request, CancellationToken cancellationToken)
    {
        var doctor = await _userRepository.GetByIdAsync(request.DoctorId);
        if (doctor is null)
            return Result<DoctorScheduleResponseDto>.Failure("Doctor not found", "doctor_not_found");

        if (request.StartTime >= request.EndTime)
            return Result<DoctorScheduleResponseDto>.Failure("Start time must be before end time", "invalid_time_range");

        if (request.SlotDurationMinutes < 5 || request.SlotDurationMinutes > 120)
            return Result<DoctorScheduleResponseDto>.Failure("Slot duration must be between 5 and 120 minutes", "invalid_slot_duration");

        var existingSchedules = await _scheduleRepository.GetByDoctorIdAsync(request.DoctorId);
        var overlap = existingSchedules.Any(s =>
            s.DayOfWeek == request.DayOfWeek &&
            s.IsActive &&
            ((request.StartTime >= s.StartTime && request.StartTime < s.EndTime) ||
             (request.EndTime > s.StartTime && request.EndTime <= s.EndTime) ||
             (request.StartTime <= s.StartTime && request.EndTime >= s.EndTime)));

        if (overlap)
            return Result<DoctorScheduleResponseDto>.Failure("Schedule overlaps with an existing schedule for this day", "schedule_overlap");

        var schedule = new DoctorSchedule
        {
            DoctorId = request.DoctorId,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            SlotDurationMinutes = request.SlotDurationMinutes,
            IsActive = true
        };

        await _scheduleRepository.AddAsync(schedule);

        return Result<DoctorScheduleResponseDto>.Success(new DoctorScheduleResponseDto(
            schedule.Id,
            schedule.DoctorId,
            $"{doctor.FirstName} {doctor.LastName}",
            schedule.DayOfWeek,
            schedule.StartTime,
            schedule.EndTime,
            schedule.SlotDurationMinutes,
            schedule.IsActive
        ));
    }
}
