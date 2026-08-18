using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.DoctorSchedules.Commands;

public record ToggleDoctorScheduleStatusCommand(Guid Id) : IRequest<Result<Unit>>;

public class ToggleDoctorScheduleStatusCommandHandler : IRequestHandler<ToggleDoctorScheduleStatusCommand, Result<Unit>>
{
    private readonly IDoctorScheduleRepository _scheduleRepository;

    public ToggleDoctorScheduleStatusCommandHandler(IDoctorScheduleRepository scheduleRepository)
    {
        _scheduleRepository = scheduleRepository;
    }

    public async Task<Result<Unit>> Handle(ToggleDoctorScheduleStatusCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(request.Id);
        if (schedule is null)
            return Result<Unit>.Failure("Schedule not found", "schedule_not_found");

        schedule.IsActive = !schedule.IsActive;
        _scheduleRepository.Update(schedule);

        return Result<Unit>.Success(Unit.Value);
    }
}
