using System.Security.Claims;
using ClinicManagementSystem.Application.Interfaces;

namespace ClinicManagementSystem.API.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var idClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User?.FindFirst("sub")?.Value
                       ?? User?.FindFirst("id")?.Value;

            return Guid.TryParse(idClaim, out var guid) ? guid : null;
        }
    }

    public string? UserName =>
        User?.FindFirst(ClaimTypes.Name)?.Value
        ?? User?.FindFirst("unique_name")?.Value
        ?? User?.Identity?.Name;

    public string? Email =>
        User?.FindFirst(ClaimTypes.Email)?.Value
        ?? User?.FindFirst("email")?.Value;

    public IReadOnlyList<string> Roles
    {
        get
        {
            if (User == null) return Array.Empty<string>();

            return User.FindAll(ClaimTypes.Role)
                .Concat(User.FindAll("role"))
                .Concat(User.FindAll("roles"))
                .Select(c => c.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool IsAdmin => IsInRole("Admin");

    public bool IsDoctor => IsInRole("Doctor");

    public bool IsRadiologist => IsInRole("Radiologist");

    public bool IsNurse => IsInRole("Nurse");

    public bool IsInRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role) || User == null) return false;

        return Roles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
    }
}
