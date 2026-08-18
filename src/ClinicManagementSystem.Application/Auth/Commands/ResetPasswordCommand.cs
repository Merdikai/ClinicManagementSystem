using ClinicManagementSystem.Application.Common;
using MediatR;

namespace ClinicManagementSystem.Application.Auth.Commands;

public record ResetPasswordCommand(string Token, string NewPassword) : IRequest<Result<Unit>>;
