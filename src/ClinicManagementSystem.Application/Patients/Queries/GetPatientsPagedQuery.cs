using ClinicManagementSystem.Application.DTOs;
using MediatR;

namespace ClinicManagementSystem.Application.Patients.Queries;

public record GetPatientsPagedQuery(int Page, int PageSize, string? Search, string? SortBy = null, bool Descending = false) : IRequest<PagedResponse<PatientResponseDto>>;
