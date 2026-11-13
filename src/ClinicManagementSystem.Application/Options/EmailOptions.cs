using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.Options;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    [Required]
    [EmailAddress]
    public string From { get; set; } = "clinic@example.com";

    public string? SmtpServer { get; set; }

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    public string? Username { get; set; }

    public string? Password { get; set; }
}

