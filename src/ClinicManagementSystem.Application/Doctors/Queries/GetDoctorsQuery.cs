using ClinicManagementSystem.Application.DTOs;
using MediatR;

namespace ClinicManagementSystem.Application.Doctors.Queries;

public record GetDoctorsQuery() : IRequest<IEnumerable<UserResponseDto>>;
