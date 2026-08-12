using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using ClinicManagementSystem.Domain.Interfaces;

namespace ClinicManagementSystem.Application.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IMapper _mapper;

    public AppointmentService(IAppointmentRepository appointmentRepository, IMapper mapper)
    {
        _appointmentRepository = appointmentRepository;
        _mapper = mapper;
    }

    public async Task<AppointmentResponseDto> CreateAsync(CreateAppointmentDto dto)
    {
        var isAvailable = await _appointmentRepository.IsSlotAvailableAsync(
            dto.DoctorId, dto.ScheduledDateTime, dto.DurationMinutes);

        if (!isAvailable)
            throw new BusinessRuleViolationException(
                "The requested time slot is not available.",
                "slot_unavailable");

        var appointment = _mapper.Map<Appointment>(dto);
        appointment.Status = AppointmentStatus.Scheduled;

        await _appointmentRepository.AddAsync(appointment);
        await _appointmentRepository.SaveChangesAsync();
        return _mapper.Map<AppointmentResponseDto>(appointment);
    }

    public async Task<AppointmentResponseDto?> GetByIdAsync(Guid id)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(id);
        if (appointment is null) throw new NotFoundException(nameof(Appointment), id);
        return _mapper.Map<AppointmentResponseDto>(appointment);
    }

    public async Task<IEnumerable<AppointmentResponseDto>> GetByDoctorAsync(Guid doctorId, DateTime? date)
    {
        var appointments = await _appointmentRepository.GetByDoctorIdAsync(doctorId, date);
        return _mapper.Map<IEnumerable<AppointmentResponseDto>>(appointments);
    }

    public async Task CheckInAsync(Guid appointmentId)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(appointmentId);
        if (appointment is null) throw new NotFoundException(nameof(Appointment), appointmentId);
        if (appointment.Status != AppointmentStatus.Scheduled)
            throw new BusinessRuleViolationException("Only scheduled appointments can be checked in.", "invalid_status");

        appointment.Status = AppointmentStatus.CheckedIn;
        _appointmentRepository.Update(appointment);
        await _appointmentRepository.SaveChangesAsync();
    }

    public async Task CancelAsync(Guid appointmentId)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(appointmentId);
        if (appointment is null) throw new NotFoundException(nameof(Appointment), appointmentId);

        appointment.Status = AppointmentStatus.Cancelled;
        _appointmentRepository.Update(appointment);
        await _appointmentRepository.SaveChangesAsync();
    }
}