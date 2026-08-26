using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using MediatR;

namespace ClinicManagementSystem.Application.Files.Queries;

public record GetPatientFilesQuery(Guid PatientId) : IRequest<Result<List<FileUploadResponseDto>>>;

public class GetPatientFilesQueryHandler : IRequestHandler<GetPatientFilesQuery, Result<List<FileUploadResponseDto>>>
{
    private readonly string _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");

    public Task<Result<List<FileUploadResponseDto>>> Handle(GetPatientFilesQuery request, CancellationToken cancellationToken)
    {
        var patientFolder = Path.Combine(_uploadPath, request.PatientId.ToString());

        if (!Directory.Exists(patientFolder))
            return Task.FromResult(Result<List<FileUploadResponseDto>>.Success(new List<FileUploadResponseDto>()));

        var files = Directory.GetFiles(patientFolder)
            .Select(f => new FileInfo(f))
            .Select(fi =>
            {
                // Disk files saved as "{GUID}__{OriginalName}" — extract display name
                var sep = new[] { "__" };
                var parts = fi.Name.Split(sep, 2, StringSplitOptions.None);
                var originalName = parts.Length > 1 ? parts[1] : fi.Name;
                return new FileUploadResponseDto(
                    Guid.NewGuid(),
                    fi.Name,
                    originalName,
                    "application/octet-stream",
                    fi.Length,
                    fi.CreationTimeUtc
                );
            })
            .ToList();

        return Task.FromResult(Result<List<FileUploadResponseDto>>.Success(files));
    }
}
