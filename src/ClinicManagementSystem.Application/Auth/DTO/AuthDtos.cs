namespace ClinicManagementSystem.Application.DTOs;

public record RegisterUserDto(string Username, string Email, string Password, string FirstName, string LastName, string Role, string? PhoneNumber);
public record LoginDto(string Username, string Password);

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
}

public record UserResponseDto(Guid Id, string Username, string Email, string FirstName, string LastName, string? PhoneNumber);
public record RefreshTokenDto(string RefreshToken);
public record LogoutDto(string RefreshToken);
public record ForgotPasswordDto(string Email);
public record ResetPasswordDto(string Token, string NewPassword);
