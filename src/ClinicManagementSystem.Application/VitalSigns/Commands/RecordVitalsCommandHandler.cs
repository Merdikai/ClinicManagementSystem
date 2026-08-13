using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.VitalSigns.Commands;

public class RecordVitalsCommandHandler : IRequestHandler<RecordVitalsCommand, VitalSignResponseDto>
{
    private readonly IVitalSignRepository _vitalSignRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IMapper _mapper;

    public RecordVitalsCommandHandler(
        IVitalSignRepository vitalSignRepository,
        IAppointmentRepository appointmentRepository,
        IMapper mapper)
    {
        _vitalSignRepository = vitalSignRepository;
        _appointmentRepository = appointmentRepository;
        _mapper = mapper;
    }

    public async Task<VitalSignResponseDto> Handle(RecordVitalsCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId)
            ?? throw new NotFoundException(nameof(Appointment), request.AppointmentId);

        var dto = new RecordVitalsDto(request.AppointmentId, request.SystolicBP, request.DiastolicBP,
            request.TemperatureC, request.HeartRateBpm, request.RespiratoryRate, request.WeightKg, request.HeightCm);
        var vitalSign = _mapper.Map<VitalSign>(dto);
        vitalSign.RecordedByNurseId = request.NurseId;

        await _vitalSignRepository.AddAsync(vitalSign);
        await _vitalSignRepository.SaveChangesAsync();
        return _mapper.Map<VitalSignResponseDto>(vitalSign);
    }
}
