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
    [Authorize(Roles = "Doctor,Nurse,Admin")]
    [EndpointSummary("Upload a patient file or lab result")]
    [ProducesResponseType(typeof(FileUploadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadFile(Guid patientId, IFormFile file, [FromForm] string documentType)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ProblemDetails { Title = "No file", Detail = "File is empty", Status = 400 });

        using var stream = file.OpenReadStream();
        var command = new UploadPatientFileCommand(stream, file.FileName, file.ContentType, file.Length, patientId, documentType);
        var result = await _sender.Send(command);

        return result.Match<IActionResult>(
            onSuccess: fileInfo => Ok(fileInfo),
            onFailure: (error, errorCode) => BadRequest(new ProblemDetails
            {
                Title = "Upload Failed",
                Detail = error,
                Status = StatusCodes.Status400BadRequest
            })
        );
    }

    [HttpGet("patient/{patientId:guid}")]
    [Authorize(Roles = "Doctor,Nurse,Admin,Patient")]
    [EndpointSummary("Get all uploaded files for a patient")]
    [ProducesResponseType(typeof(List<FileUploadResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPatientFiles(Guid patientId)
    {
        var result = await _sender.Send(new GetPatientFilesQuery(patientId));
        return Ok(result.Value);
    }
}
