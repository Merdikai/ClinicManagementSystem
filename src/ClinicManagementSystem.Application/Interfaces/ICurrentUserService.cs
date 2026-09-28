namespace ClinicManagementSystem.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserName { get; }
    string? Email { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    bool IsDoctor { get; }
    bool IsRadiologist { get; }
    bool IsNurse { get; }
    bool IsInRole(string role);
}
