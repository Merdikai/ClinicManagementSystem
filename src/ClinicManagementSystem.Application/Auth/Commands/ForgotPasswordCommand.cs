using ClinicManagementSystem.Application.Common;
using MediatR;

namespace ClinicManagementSystem.Application.Auth.Commands;

public record ForgotPasswordCommand(string Email) : IRequest<Result<Unit>>;
