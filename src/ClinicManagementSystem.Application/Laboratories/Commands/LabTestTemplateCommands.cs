using System;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Laboratories.DTOs;
using ClinicManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Laboratories.Commands;

public record CreateLabTestTemplateCommand(CreateLabTestTemplateDto Dto) : IRequest<Result<LabTestTemplateDto>>;

public class CreateLabTestTemplateCommandHandler : IRequestHandler<CreateLabTestTemplateCommand, Result<LabTestTemplateDto>>
{
    private readonly IClinicDbContext _context;

    public CreateLabTestTemplateCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<LabTestTemplateDto>> Handle(CreateLabTestTemplateCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        if (string.IsNullOrWhiteSpace(dto.TestCode))
            return Result<LabTestTemplateDto>.Failure("Test code is required.");
        if (string.IsNullOrWhiteSpace(dto.TestName))
            return Result<LabTestTemplateDto>.Failure("Test name is required.");

        var exists = await _context.LabTestTemplates
            .AnyAsync(t => t.TestCode.ToLower() == dto.TestCode.Trim().ToLower(), cancellationToken);

        if (exists)
            return Result<LabTestTemplateDto>.Failure($"A test panel with code '{dto.TestCode}' already exists.");

        var template = new LabTestTemplate
        {
            Id = Guid.NewGuid(),
            TestCode = dto.TestCode.Trim().ToUpperInvariant(),
            TestName = dto.TestName.Trim(),
            Category = string.IsNullOrWhiteSpace(dto.Category) ? "General" : dto.Category.Trim(),
            Description = dto.Description?.Trim(),
            SampleType = string.IsNullOrWhiteSpace(dto.SampleType) ? "Blood" : dto.SampleType.Trim(),
            TurnaroundTimeHours = dto.TurnaroundTimeHours > 0 ? dto.TurnaroundTimeHours : 24,
            Price = dto.Price >= 0 ? dto.Price : 0,
            IsActive = true,
            ParametersJson = string.IsNullOrWhiteSpace(dto.ParametersJson) ? "[]" : dto.ParametersJson.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.LabTestTemplates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);

        var resultDto = new LabTestTemplateDto
        {
            Id = template.Id,
            TestCode = template.TestCode,
            TestName = template.TestName,
            Category = template.Category,
            Description = template.Description,
            SampleType = template.SampleType,
            TurnaroundTimeHours = template.TurnaroundTimeHours,
            Price = template.Price,
            IsActive = template.IsActive,
            ParametersJson = template.ParametersJson
        };

        return Result<LabTestTemplateDto>.Success(resultDto);
    }
}

public record UpdateLabTestTemplateCommand(Guid Id, UpdateLabTestTemplateDto Dto) : IRequest<Result<LabTestTemplateDto>>;

public class UpdateLabTestTemplateCommandHandler : IRequestHandler<UpdateLabTestTemplateCommand, Result<LabTestTemplateDto>>
{
    private readonly IClinicDbContext _context;

    public UpdateLabTestTemplateCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<LabTestTemplateDto>> Handle(UpdateLabTestTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _context.LabTestTemplates
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (template == null)
            return Result<LabTestTemplateDto>.Failure("Diagnostic test template not found.");

        var dto = request.Dto;
        if (string.IsNullOrWhiteSpace(dto.TestCode))
            return Result<LabTestTemplateDto>.Failure("Test code is required.");
        if (string.IsNullOrWhiteSpace(dto.TestName))
            return Result<LabTestTemplateDto>.Failure("Test name is required.");

        var duplicateCode = await _context.LabTestTemplates
            .AnyAsync(t => t.Id != request.Id && t.TestCode.ToLower() == dto.TestCode.Trim().ToLower(), cancellationToken);

        if (duplicateCode)
            return Result<LabTestTemplateDto>.Failure($"Another test panel with code '{dto.TestCode}' already exists.");

        template.TestCode = dto.TestCode.Trim().ToUpperInvariant();
        template.TestName = dto.TestName.Trim();
        template.Category = string.IsNullOrWhiteSpace(dto.Category) ? "General" : dto.Category.Trim();
        template.Description = dto.Description?.Trim();
        template.SampleType = string.IsNullOrWhiteSpace(dto.SampleType) ? "Blood" : dto.SampleType.Trim();
        template.TurnaroundTimeHours = dto.TurnaroundTimeHours > 0 ? dto.TurnaroundTimeHours : 24;
        template.Price = dto.Price >= 0 ? dto.Price : 0;
        template.IsActive = dto.IsActive;
        if (!string.IsNullOrWhiteSpace(dto.ParametersJson))
        {
            template.ParametersJson = dto.ParametersJson.Trim();
        }

        await _context.SaveChangesAsync(cancellationToken);

        var resultDto = new LabTestTemplateDto
        {
            Id = template.Id,
            TestCode = template.TestCode,
            TestName = template.TestName,
            Category = template.Category,
            Description = template.Description,
            SampleType = template.SampleType,
            TurnaroundTimeHours = template.TurnaroundTimeHours,
            Price = template.Price,
            IsActive = template.IsActive,
            ParametersJson = template.ParametersJson
        };

        return Result<LabTestTemplateDto>.Success(resultDto);
    }
}

public record DeleteLabTestTemplateCommand(Guid Id) : IRequest<Result<bool>>;

public class DeleteLabTestTemplateCommandHandler : IRequestHandler<DeleteLabTestTemplateCommand, Result<bool>>
{
    private readonly IClinicDbContext _context;

    public DeleteLabTestTemplateCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(DeleteLabTestTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _context.LabTestTemplates
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (template == null)
            return Result<bool>.Failure("Diagnostic test template not found.");

        var hasOrders = await _context.LabOrderItems
            .AnyAsync(i => i.LabTestTemplateId == request.Id, cancellationToken);

        if (hasOrders)
        {
            // Soft delete so historical patient order history and reports remain intact
            template.IsActive = false;
        }
        else
        {
            // Hard delete if never ordered
            _context.LabTestTemplates.Remove(template);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
