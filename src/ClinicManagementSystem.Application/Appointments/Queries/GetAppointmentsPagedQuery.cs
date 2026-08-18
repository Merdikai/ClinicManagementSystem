using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using MediatR;

namespace ClinicManagementSystem.Application.Appointments.Queries;

public record GetAppointmentsPagedQuery(int Page, int PageSize, DateTime? StartDate, DateTime? EndDate) 
    : IRequest<PagedResponse<AppointmentResponseDto>>;
