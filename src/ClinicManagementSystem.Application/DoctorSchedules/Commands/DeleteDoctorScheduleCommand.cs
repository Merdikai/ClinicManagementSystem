using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.DoctorSchedules.Commands;

public record DeleteDoctorScheduleCommand(Guid Id) : IRequest<Result<Unit>>;

public class DeleteDoctorScheduleCommandHandler : IRequestHandler<DeleteDoctorScheduleCommand, Result<Unit>>
{
    private readonly IDoctorScheduleRepository _scheduleRepository;

    public DeleteDoctorScheduleCommandHandler(IDoctorScheduleRepository scheduleRepository)
    {
        _scheduleRepository = scheduleRepository;
    }

    public async Task<Result<Unit>> Handle(DeleteDoctorScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(request.Id);
        if (schedule is null)
            return Result<Unit>.Failure("Schedule not found", "schedule_not_found");

        _scheduleRepository.Delete(schedule);
        return Result<Unit>.Success(Unit.Value);
    }
}
