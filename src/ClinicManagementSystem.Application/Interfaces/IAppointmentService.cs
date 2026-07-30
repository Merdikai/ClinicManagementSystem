using ClinicManagementSystem.Application.DTOs;

namespace ClinicManagementSystem.Application.Interfaces;

public interface IAppointmentService
{
    Task<AppointmentResponseDto> CreateAsync(CreateAppointmentDto dto);
    Task<AppointmentResponseDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<AppointmentResponseDto>> GetByDoctorAsync(Guid doctorId, DateTime? date);
    Task CheckInAsync(Guid appointmentId);
    Task CancelAsync(Guid appointmentId);
}