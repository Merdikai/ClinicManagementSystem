namespace ClinicManagementSystem.Application.DTOs;

public record FileUploadResponseDto(
    Guid Id,
    string FileName,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    DateTime UploadedAt,
    string? DownloadUrl = null
);
