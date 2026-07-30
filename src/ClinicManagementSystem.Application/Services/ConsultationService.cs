using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;

namespace ClinicManagementSystem.Application.Services;

public class ConsultationService : IConsultationService
{
    private readonly IConsultationRepository _consultationRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IMapper _mapper;

    public ConsultationService(
        IConsultationRepository consultationRepository,
        IAppointmentRepository appointmentRepository,
        IMapper mapper)
    {
        _consultationRepository = consultationRepository;
        _appointmentRepository = appointmentRepository;
        _mapper = mapper;
    }

    public async Task<ConsultationResponseDto> CreateAsync(CreateConsultationDto dto, Guid doctorId)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(dto.AppointmentId);
        if (appointment is null)
            throw new NotFoundException(nameof(Appointment), dto.AppointmentId);

        var consultation = _mapper.Map<Consultation>(dto);
        consultation.DoctorId = doctorId;

        await _consultationRepository.AddAsync(consultation);
        return _mapper.Map<ConsultationResponseDto>(consultation);
    }
}