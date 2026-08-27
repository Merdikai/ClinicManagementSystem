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

        // Resolve a valid nurse/user ID
        Guid validNurseId = request.NurseId;
        var nurse = validNurseId != Guid.Empty ? await _userRepository.GetByIdAsync(validNurseId) : null;
        if (nurse is null)
        {
            var allUsers = await _userRepository.GetAllAsync();
            var fallbackUser = allUsers.FirstOrDefault();

            if (fallbackUser != null)
                validNurseId = fallbackUser.Id;
            else if (appointment.DoctorId != Guid.Empty)
                validNurseId = appointment.DoctorId;
        }

        // Check if vital sign already exists for this appointment
        var existingVitals = await _vitalSignRepository.GetByAppointmentIdAsync(request.AppointmentId);
        if (existingVitals != null)
        {
            existingVitals.SystolicBP = request.SystolicBP;
            existingVitals.DiastolicBP = request.DiastolicBP;
            existingVitals.TemperatureC = request.TemperatureC;
            existingVitals.HeartRateBpm = request.HeartRateBpm;
            existingVitals.RespiratoryRate = request.RespiratoryRate;
            existingVitals.WeightKg = request.WeightKg;
            existingVitals.HeightCm = request.HeightCm;
            existingVitals.RecordedByNurseId = validNurseId;
            existingVitals.RecordedAt = DateTime.UtcNow;

            await _vitalSignRepository.SaveChangesAsync();
            return Result<VitalSignResponseDto>.Success(_mapper.Map<VitalSignResponseDto>(existingVitals));
        }

        var dto = new RecordVitalsDto(request.AppointmentId, request.SystolicBP, request.DiastolicBP,
            request.TemperatureC, request.HeartRateBpm, request.RespiratoryRate, request.WeightKg, request.HeightCm);
        var vitalSign = _mapper.Map<VitalSign>(dto);
        vitalSign.RecordedByNurseId = validNurseId;

        await _vitalSignRepository.AddAsync(vitalSign);
        await _vitalSignRepository.SaveChangesAsync();
        return Result<VitalSignResponseDto>.Success(_mapper.Map<VitalSignResponseDto>(vitalSign));
    }
}
