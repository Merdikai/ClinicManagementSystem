namespace ClinicManagementSystem.Application.DTOs;

public record AssignRoleDto(Guid UserId, string RoleName);
public record RemoveRoleDto(Guid UserId, string RoleName);
public record UserWithRolesDto(
    Guid UserId,
    string Username,
    string FullName,
    List<string> Roles
);
