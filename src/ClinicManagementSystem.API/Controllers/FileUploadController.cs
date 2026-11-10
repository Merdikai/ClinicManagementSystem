using Asp.Versioning;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Files.Commands;
using ClinicManagementSystem.Application.Files.Queries;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/files")]
[Tags("Files")]
[Authorize]
public class FileUploadController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IClinicDbContext _context;

    public FileUploadController(ISender sender, IClinicDbContext context)
    {
        _sender = sender;
        _context = context;
    }

    private async Task<bool> IsPatientAuthorizedAsync(Guid patientId)
    {
        if (!User.IsInRole("Patient")) return true;

        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;
        var username = User.FindFirst(ClaimTypes.Name)?.Value;

        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(username))
            return false;

        var patient = await _context.Patients.FirstOrDefaultAsync(p =>
            (!string.IsNullOrEmpty(email) && p.Email == email) ||
            (!string.IsNullOrEmpty(username) && p.Email == username));

        return patient != null && patient.Id == patientId;
    }

    [HttpPost("upload/{patientId:guid}")]
    [Authorize(Roles = "Doctor,Nurse,Admin,Receptionist,Patient")]
    [EndpointSummary("Upload a patient file or lab result")]
    [ProducesResponseType(typeof(FileUploadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UploadFile(Guid patientId, IFormFile file, [FromForm] string? documentType = "LabReport")
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ProblemDetails { Title = "No file", Detail = "File is empty", Status = 400 });

        if (!await IsPatientAuthorizedAsync(patientId))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = "Patients are only authorized to upload to their own clinical record.",
                Status = StatusCodes.Status403Forbidden
            });
        }

        using var stream = file.OpenReadStream();
        var command = new UploadPatientFileCommand(stream, file.FileName, file.ContentType, file.Length, patientId, documentType ?? "LabReport");
        var result = await _sender.Send(command);

        return result.Match<IActionResult>(
            onSuccess: fileInfo =>
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}/api/v1";
                var withUrl = fileInfo with { DownloadUrl = $"{baseUrl}/files/download/{patientId}/{fileInfo.FileName}" };
                return Ok(withUrl);
            },
            onFailure: (error, errorCode) => BadRequest(new ProblemDetails
            {
                Title = "Upload Failed",
                Detail = error,
                Status = StatusCodes.Status400BadRequest
            })
        );
    }

    [HttpGet("patient/{patientId:guid}")]
    [Authorize(Roles = "Doctor,Nurse,Admin,Receptionist,Patient")]
    [EndpointSummary("Get all uploaded files for a patient")]
    [ProducesResponseType(typeof(List<FileUploadResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPatientFiles(Guid patientId)
    {
        if (!await IsPatientAuthorizedAsync(patientId))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = "Patients are only authorized to view files from their own clinical record.",
                Status = StatusCodes.Status403Forbidden
            });
        }

        var result = await _sender.Send(new GetPatientFilesQuery(patientId));
        var baseUrl = $"{Request.Scheme}://{Request.Host}/api/v1";
        var files = (result.Value ?? new List<FileUploadResponseDto>())
            .Select(f => f with { DownloadUrl = $"{baseUrl}/files/download/{patientId}/{f.FileName}" })
            .ToList();
        return Ok(files);
    }

    [HttpGet("download/{patientId:guid}/{fileName}")]
    [Authorize(Roles = "Doctor,Nurse,Admin,Receptionist,Patient")]
    [EndpointSummary("Download a patient file")]
    public async Task<IActionResult> DownloadFile(Guid patientId, string fileName)
    {
        if (!await IsPatientAuthorizedAsync(patientId))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = "Patients are only authorized to download files from their own clinical record.",
                Status = StatusCodes.Status403Forbidden
            });
        }

        // Prevent Path Traversal
        var safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrEmpty(safeFileName) || safeFileName != fileName)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid File Name", Detail = "File name contains invalid path characters.", Status = 400 });
        }

        var uploadPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "uploads"));
        var patientFolder = Path.GetFullPath(Path.Combine(uploadPath, patientId.ToString()));
        var filePath = Path.GetFullPath(Path.Combine(patientFolder, safeFileName));

        if (!filePath.StartsWith(patientFolder, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ProblemDetails { Title = "Security Violation", Detail = "Invalid file path traversal attempt.", Status = 400 });
        }

        if (!System.IO.File.Exists(filePath))
            return NotFound(new ProblemDetails { Title = "File not found", Status = 404 });

        var ext = Path.GetExtension(safeFileName).ToLower();
        var contentType = ext switch
        {
            ".pdf"  => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png"  => "image/png",
            ".doc"  => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _       => "application/octet-stream"
        };

        // Extract original display name from "{GUID}__{OriginalName}" format
        var sep = new[] { "__" };
        var parts = safeFileName.Split(sep, 2, StringSplitOptions.None);
        var downloadName = parts.Length > 1 ? parts[1] : safeFileName;

        // Efficient kernel streaming with HTTP Range processing (avoids byte[] memory buffers)
        return PhysicalFile(filePath, contentType, downloadName, enableRangeProcessing: true);
    }
}
