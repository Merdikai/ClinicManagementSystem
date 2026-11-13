using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    [MinLength(32, ErrorMessage = "Jwt:Key must be at least 32 characters long.")]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int ExpiryMinutes { get; set; } = 15;

    [Range(1, 90)]
    public int RefreshTokenExpiryDays { get; set; } = 7;
}

