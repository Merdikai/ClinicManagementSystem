namespace ClinicManagementSystem.Application.DTOs;

// Request
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

// Response
public record AuthResponseDto(
    string Token,
    string Username,
    string FullName,
    IList<string> Roles
);