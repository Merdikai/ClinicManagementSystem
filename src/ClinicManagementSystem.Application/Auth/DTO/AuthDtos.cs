namespace ClinicManagementSystem.Application.DTOs;

public record RegisterUserDto(
    string Username,
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string PhoneNumber
);

public record LoginDto(
    string Username,
    string Password
);

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
}

public record UserResponseDto(
    Guid Id,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string PhoneNumber
);
