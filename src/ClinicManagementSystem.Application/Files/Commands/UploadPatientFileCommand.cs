using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using MediatR;

namespace ClinicManagementSystem.Application.Files.Commands;

public record UploadPatientFileCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSize,
    Guid PatientId,
    string DocumentType
) : IRequest<Result<FileUploadResponseDto>>;

public class UploadPatientFileCommandHandler : IRequestHandler<UploadPatientFileCommand, Result<FileUploadResponseDto>>
{
    private readonly string _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");

    public UploadPatientFileCommandHandler()
    {
        if (!Directory.Exists(_uploadPath))
            Directory.CreateDirectory(_uploadPath);
    }

    public async Task<Result<FileUploadResponseDto>> Handle(UploadPatientFileCommand request, CancellationToken cancellationToken)
    {
        if (request.FileStream is null || request.FileSize == 0)
            return Result<FileUploadResponseDto>.Failure("No file uploaded", "no_file");

        var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
        var extension = Path.GetExtension(request.FileName).ToLower();

        if (!allowedExtensions.Contains(extension))
            return Result<FileUploadResponseDto>.Failure("File type not allowed", "invalid_file_type");

        if (request.FileSize > 10 * 1024 * 1024) // 10MB limit
            return Result<FileUploadResponseDto>.Failure("File too large (max 10MB)", "file_too_large");

        var patientFolder = Path.Combine(_uploadPath, request.PatientId.ToString());
        if (!Directory.Exists(patientFolder))
            Directory.CreateDirectory(patientFolder);

        var savedFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(patientFolder, savedFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await request.FileStream.CopyToAsync(stream, cancellationToken);
        }

        var response = new FileUploadResponseDto(
            Guid.NewGuid(),
            request.FileName,
            request.ContentType,
            request.FileSize,
            DateTime.UtcNow
        );

        return Result<FileUploadResponseDto>.Success(response);
    }
}
