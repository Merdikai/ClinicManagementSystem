using AutoMapper;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.VitalSigns.Commands;

public class RecordVitalsCommandHandler : IRequestHandler<RecordVitalsCommand, Result<VitalSignResponseDto>>
{
    private readonly IVitalSignRepository _vitalSignRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public RecordVitalsCommandHandler(
        IVitalSignRepository vitalSignRepository,
        IAppointmentRepository appointmentRepository,
        IUserRepository userRepository,
        IMapper mapper)
    {
        _vitalSignRepository = vitalSignRepository;
        _appointmentRepository = appointmentRepository;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<Result<VitalSignResponseDto>> Handle(RecordVitalsCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId);
        if (appointment is null)
            return Result<VitalSignResponseDto>.Failure($"Appointment {request.AppointmentId} not found", "appointment_not_found");

        // Resolve a valid user ID
        Guid validUserId = request.UserId;
        var user = validUserId != Guid.Empty ? await _userRepository.GetByIdAsync(validUserId) : null;
        if (user is null)
        {
            var allUsers = await _userRepository.GetAllAsync();
            var fallbackUser = allUsers.FirstOrDefault();

            if (fallbackUser != null)
                validUserId = fallbackUser.Id;
            else if (appointment.DoctorId != Guid.Empty)
                validUserId = appointment.DoctorId;
        }

        // Always append a new vital signs entry with current timestamp to preserve full clinical progression
        var newVitals = new VitalSign
        {
            Id = Guid.NewGuid(),
            AppointmentId = request.AppointmentId,
            RecordedByUserId = validUserId,
            ConsultationId = request.ConsultationId,
            SystolicBP = request.SystolicBP,
            DiastolicBP = request.DiastolicBP,
            TemperatureC = request.TemperatureC,
            HeartRateBpm = request.HeartRateBpm,
            RespiratoryRate = request.RespiratoryRate,
            WeightKg = request.WeightKg,
            HeightCm = request.HeightCm,
            OxygenSaturation = request.OxygenSaturation,
            PainScore = request.PainScore,
            RecordedAt = DateTime.UtcNow
        };

        await _vitalSignRepository.AddAsync(newVitals);
        await _vitalSignRepository.SaveChangesAsync();
        return Result<VitalSignResponseDto>.Success(_mapper.Map<VitalSignResponseDto>(newVitals));
    }
}
