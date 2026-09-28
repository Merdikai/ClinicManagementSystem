using System;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Domain.Enums;
using MediatR;

namespace ClinicManagementSystem.Application.Appointments.Queries;

public record GetAppointmentsPagedQuery(
    int Page = 1,
    int PageSize = 10,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    Guid? PatientId = null,
    AppointmentStatus? Status = null
) : IRequest<PagedResponse<AppointmentResponseDto>>;
