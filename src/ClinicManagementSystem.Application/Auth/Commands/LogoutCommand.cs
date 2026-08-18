using MediatR;

namespace ClinicManagementSystem.Application.Auth.Commands;

public record LogoutCommand(string RefreshToken) : IRequest<Unit>;
