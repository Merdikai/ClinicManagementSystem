using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;

namespace ClinicManagementSystem.Application.Services;

public class VitalSignService : IVitalSignService
{
    private readonly IVitalSignRepository _vitalSignRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IMapper _mapper;

    public VitalSignService(
        IVitalSignRepository vitalSignRepository,
        IAppointmentRepository appointmentRepository,
        IMapper mapper)
    {
        _vitalSignRepository = vitalSignRepository;
        _appointmentRepository = appointmentRepository;
        _mapper = mapper;
    }

    public async Task<VitalSignResponseDto> RecordAsync(RecordVitalsDto dto, Guid nurseId)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(dto.AppointmentId);
        if (appointment is null)
            throw new NotFoundException(nameof(Appointment), dto.AppointmentId);

        var vitalSign = _mapper.Map<VitalSign>(dto);
        vitalSign.RecordedByNurseId = nurseId;

        await _vitalSignRepository.AddAsync(vitalSign);
        await _vitalSignRepository.SaveChangesAsync();
        return _mapper.Map<VitalSignResponseDto>(vitalSign);
    }
}