using ClinicManagementSystem.Application.DTOs;
using MediatR;

namespace ClinicManagementSystem.Application.Reports.Queries;

public record GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>;
