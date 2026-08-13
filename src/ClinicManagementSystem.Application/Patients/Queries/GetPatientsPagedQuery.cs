using ClinicManagementSystem.Application.DTOs;
using MediatR;

namespace ClinicManagementSystem.Application.Patients.Queries;

public record GetPatientsPagedQuery(int Page, int PageSize, string? Search) : IRequest<PagedResponse<PatientResponseDto>>;
