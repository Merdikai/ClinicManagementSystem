namespace ClinicManagementSystem.Application.DTOs;

public record LinkDto(
    string Rel,
    string Href,
    string Method,
    string? Description = null
);
