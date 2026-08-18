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
            .Select(fi => new FileUploadResponseDto(
                Guid.NewGuid(),
                fi.Name,
                "application/octet-stream",
                fi.Length,
                fi.CreationTimeUtc
            ))
            .ToList();

        return Task.FromResult(Result<List<FileUploadResponseDto>>.Success(files));
    }
}
