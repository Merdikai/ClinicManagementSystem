using ClinicManagementSystem.Application.DTOs;
using MediatR;

namespace ClinicManagementSystem.Application.Patients.Queries;

public record GetPatientByIdQuery(Guid Id) : IRequest<PatientResponseDto>;
