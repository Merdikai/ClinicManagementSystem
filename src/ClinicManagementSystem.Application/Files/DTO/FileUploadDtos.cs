namespace ClinicManagementSystem.Application.DTOs;

public record FileUploadResponseDto(
    Guid Id,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    DateTime UploadedAt
);
