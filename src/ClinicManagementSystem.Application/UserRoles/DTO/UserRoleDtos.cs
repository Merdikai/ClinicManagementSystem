namespace ClinicManagementSystem.Application.DTOs;

public record AssignRoleDto(Guid UserId, string RoleName);
public record RemoveRoleDto(Guid UserId, string RoleName);

public record AdminCreateUserDto(
    string UserName,
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string RoleName,
    string? PhoneNumber);

public record AdminResetPasswordDto(string NewPassword);

public class UserWithRolesDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; set; } = new();

    public UserWithRolesDto() { }

    public UserWithRolesDto(
        Guid userId,
        string username,
        string email,
        string firstName,
        string lastName,
        string? phoneNumber,
        bool isActive,
        DateTime createdAt,
        List<string> roles)
    {
        UserId = userId;
        Username = username;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        IsActive = isActive;
        CreatedAt = createdAt;
        Roles = roles;
    }
}
