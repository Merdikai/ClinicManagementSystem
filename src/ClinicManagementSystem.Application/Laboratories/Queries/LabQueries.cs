using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Laboratories.DTOs;
using ClinicManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Laboratories.Queries;

public record GetLabTestTemplatesQuery(string? Category = null) : IRequest<Result<List<LabTestTemplateDto>>>;

public class GetLabTestTemplatesQueryHandler : IRequestHandler<GetLabTestTemplatesQuery, Result<List<LabTestTemplateDto>>>
{
    private readonly IClinicDbContext _context;

    public GetLabTestTemplatesQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<LabTestTemplateDto>>> Handle(GetLabTestTemplatesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.LabTestTemplates.AsNoTracking().Where(t => t.IsActive);
        if (!string.IsNullOrWhiteSpace(request.Category))
            query = query.Where(t => t.Category == request.Category);

        var list = await query
            .OrderBy(t => t.Category)
            .ThenBy(t => t.TestName)
            .Select(t => new LabTestTemplateDto
            {
                Id = t.Id,
                TestCode = t.TestCode,
                TestName = t.TestName,
                Category = t.Category,
                Description = t.Description,
                SampleType = t.SampleType,
                TurnaroundTimeHours = t.TurnaroundTimeHours,
                Price = t.Price,
                IsActive = t.IsActive,
                ParametersJson = t.ParametersJson
            })
            .ToListAsync(cancellationToken);

        return Result<List<LabTestTemplateDto>>.Success(list);
    }
}

public record GetLabOrdersPagedQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    string? Status = null,
    string? Priority = null,
    Guid? PatientId = null
) : IRequest<Result<PagedResponse<LabOrderDto>>>;

public class GetLabOrdersPagedQueryHandler : IRequestHandler<GetLabOrdersPagedQuery, Result<PagedResponse<LabOrderDto>>>
{
    private readonly IClinicDbContext _context;

    public GetLabOrdersPagedQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResponse<LabOrderDto>>> Handle(GetLabOrdersPagedQuery request, CancellationToken cancellationToken)
    {
        var query = _context.LabOrders
            .AsNoTracking()
            .Include(o => o.Patient)
            .Include(o => o.Doctor)
            .Include(o => o.Items)
                .ThenInclude(i => i.LabTestTemplate)
            .Include(o => o.Items)
                .ThenInclude(i => i.Result)
            .AsQueryable();

        if (request.PatientId.HasValue)
            query = query.Where(o => o.PatientId == request.PatientId.Value);

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<LabOrderStatus>(request.Status, true, out var status))
            query = query.Where(o => o.Status == status);

        if (!string.IsNullOrWhiteSpace(request.Priority) && Enum.TryParse<LabOrderPriority>(request.Priority, true, out var priority))
            query = query.Where(o => o.Priority == priority);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLower();
            query = query.Where(o =>
                o.OrderNumber.ToLower().Contains(s) ||
                o.Patient.FirstName.ToLower().Contains(s) ||
                o.Patient.LastName.ToLower().Contains(s) ||
                o.Patient.MedicalRecordNumber.ToLower().Contains(s));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(o => new LabOrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                PatientId = o.PatientId,
                PatientName = $"{o.Patient.FirstName} {o.Patient.LastName}",
                PatientMedicalRecordNumber = o.Patient.MedicalRecordNumber,
                DoctorId = o.DoctorId,
                DoctorName = $"{o.Doctor.FirstName} {o.Doctor.LastName}",
                AppointmentId = o.AppointmentId,
                OrderDate = o.OrderDate,
                Status = o.Status.ToString(),
                Priority = o.Priority.ToString(),
                ClinicalNotes = o.ClinicalNotes,
                TotalCost = o.TotalCost,
                IsBilled = o.IsBilled,
                SampleCollectedAt = o.SampleCollectedAt,
                CompletedAt = o.CompletedAt,
                Items = o.Items.Select(i => new LabOrderItemDto
                {
                    Id = i.Id,
                    LabOrderId = o.Id,
                    LabTestTemplateId = i.LabTestTemplateId,
                    TestCode = i.LabTestTemplate.TestCode,
                    TestName = i.LabTestTemplate.TestName,
                    Category = i.LabTestTemplate.Category,
                    SampleType = i.LabTestTemplate.SampleType,
                    Status = i.Status.ToString(),
                    Price = i.Price,
                    Result = i.Result == null ? null : new LabResultDto
                    {
                        Id = i.Result.Id,
                        LabOrderItemId = i.Result.LabOrderItemId,
                        PerformedByTechnicianId = i.Result.PerformedByTechnicianId,
                        ResultDate = i.Result.ResultDate,
                        ParameterResultsJson = i.Result.ParameterResultsJson,
                        Remarks = i.Result.Remarks,
                        HasAbnormalFlag = i.Result.HasAbnormalFlag,
                        AttachmentUrl = i.Result.AttachmentUrl
                    }
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
        var response = new PagedResponse<LabOrderDto>(
            items, totalCount, request.Page, request.PageSize,
            totalPages,
            request.Page < totalPages,
            request.Page > 1
        );
        return Result<PagedResponse<LabOrderDto>>.Success(response);
    }
}

public record GetLabOrdersByPatientQuery(Guid PatientId) : IRequest<Result<List<LabOrderDto>>>;

public class GetLabOrdersByPatientQueryHandler : IRequestHandler<GetLabOrdersByPatientQuery, Result<List<LabOrderDto>>>
{
    private readonly IClinicDbContext _context;

    public GetLabOrdersByPatientQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<LabOrderDto>>> Handle(GetLabOrdersByPatientQuery request, CancellationToken cancellationToken)
    {
        var orders = await _context.LabOrders
            .AsNoTracking()
            .Include(o => o.Patient)
            .Include(o => o.Doctor)
            .Include(o => o.Items)
                .ThenInclude(i => i.LabTestTemplate)
            .Include(o => o.Items)
                .ThenInclude(i => i.Result)
            .Where(o => o.PatientId == request.PatientId)
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new LabOrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                PatientId = o.PatientId,
                PatientName = $"{o.Patient.FirstName} {o.Patient.LastName}",
                PatientMedicalRecordNumber = o.Patient.MedicalRecordNumber,
                DoctorId = o.DoctorId,
                DoctorName = $"{o.Doctor.FirstName} {o.Doctor.LastName}",
                AppointmentId = o.AppointmentId,
                OrderDate = o.OrderDate,
                Status = o.Status.ToString(),
                Priority = o.Priority.ToString(),
                ClinicalNotes = o.ClinicalNotes,
                TotalCost = o.TotalCost,
                IsBilled = o.IsBilled,
                SampleCollectedAt = o.SampleCollectedAt,
                CompletedAt = o.CompletedAt,
                Items = o.Items.Select(i => new LabOrderItemDto
                {
                    Id = i.Id,
                    LabOrderId = o.Id,
                    LabTestTemplateId = i.LabTestTemplateId,
                    TestCode = i.LabTestTemplate.TestCode,
                    TestName = i.LabTestTemplate.TestName,
                    Category = i.LabTestTemplate.Category,
                    SampleType = i.LabTestTemplate.SampleType,
                    Status = i.Status.ToString(),
                    Price = i.Price,
                    Result = i.Result == null ? null : new LabResultDto
                    {
                        Id = i.Result.Id,
                        LabOrderItemId = i.Result.LabOrderItemId,
                        PerformedByTechnicianId = i.Result.PerformedByTechnicianId,
                        ResultDate = i.Result.ResultDate,
                        ParameterResultsJson = i.Result.ParameterResultsJson,
                        Remarks = i.Result.Remarks,
                        HasAbnormalFlag = i.Result.HasAbnormalFlag,
                        AttachmentUrl = i.Result.AttachmentUrl
                    }
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        return Result<List<LabOrderDto>>.Success(orders);
    }
}

public record GetLabMetricsQuery : IRequest<Result<LabMetricsDto>>;

public class GetLabMetricsQueryHandler : IRequestHandler<GetLabMetricsQuery, Result<LabMetricsDto>>
{
    private readonly IClinicDbContext _context;

    public GetLabMetricsQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<LabMetricsDto>> Handle(GetLabMetricsQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;

        var pending = await _context.LabOrders.CountAsync(o => o.Status == LabOrderStatus.Ordered, cancellationToken);
        var collected = await _context.LabOrders.CountAsync(o => o.Status == LabOrderStatus.SampleCollected, cancellationToken);
        var inProgress = await _context.LabOrders.CountAsync(o => o.Status == LabOrderStatus.InProgress, cancellationToken);
        var completedToday = await _context.LabOrders.CountAsync(o => o.Status == LabOrderStatus.Completed && o.CompletedAt >= today, cancellationToken);
        var total = await _context.LabOrders.CountAsync(cancellationToken);

        return Result<LabMetricsDto>.Success(new LabMetricsDto
        {
            PendingOrders = pending,
            SamplesCollected = collected,
            InProgress = inProgress,
            CompletedToday = completedToday,
            TotalTests = total
        });
    }
}


