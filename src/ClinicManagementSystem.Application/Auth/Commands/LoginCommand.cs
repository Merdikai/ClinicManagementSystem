using ClinicManagementSystem.Application.DTOs;
using MediatR;

namespace ClinicManagementSystem.Application.Auth.Commands;

public record LoginCommand(
    string Username,
    string Password
) : IRequest<AuthResponseDto>;
