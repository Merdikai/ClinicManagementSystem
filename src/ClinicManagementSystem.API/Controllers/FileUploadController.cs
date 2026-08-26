using Asp.Versioning;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Files.Commands;
using ClinicManagementSystem.Application.Files.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/files")]
[Tags("Files")]
[Authorize]
public class FileUploadController : ControllerBase
{
    private readonly ISender _sender;

    public FileUploadController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("upload/{patientId:guid}")]
    [Authorize(Roles = "Doctor,Nurse,Admin,Receptionist,Patient")]
    [EndpointSummary("Upload a patient file or lab result")]
    [ProducesResponseType(typeof(FileUploadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadFile(Guid patientId, IFormFile file, [FromForm] string? documentType = "LabReport")
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ProblemDetails { Title = "No file", Detail = "File is empty", Status = 400 });

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
    public async Task<IActionResult> GetPatientFiles(Guid patientId)
    {
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
    public IActionResult DownloadFile(Guid patientId, string fileName)
    {
        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        var filePath = Path.Combine(uploadPath, patientId.ToString(), fileName);

        if (!System.IO.File.Exists(filePath))
            return NotFound(new ProblemDetails { Title = "File not found", Status = 404 });

        var ext = Path.GetExtension(fileName).ToLower();
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
        var parts = fileName.Split(sep, 2, StringSplitOptions.None);
        var downloadName = parts.Length > 1 ? parts[1] : fileName;

        var bytes = System.IO.File.ReadAllBytes(filePath);
        return File(bytes, contentType, downloadName);
    }
}
