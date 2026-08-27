using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Laboratories.DTOs;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Laboratories.Commands;

public record CreateLabOrderCommand(CreateLabOrderRequest Request, Guid CurrentUserId) : IRequest<Result<LabOrderDto>>;

public class CreateLabOrderCommandHandler : IRequestHandler<CreateLabOrderCommand, Result<LabOrderDto>>
{
    private readonly IClinicDbContext _context;

    public CreateLabOrderCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<LabOrderDto>> Handle(CreateLabOrderCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var patient = await _context.Patients.FindAsync(new object[] { req.PatientId }, cancellationToken);
        if (patient is null)
            return Result<LabOrderDto>.Failure("Patient not found");

        var doctorId = req.DoctorId ?? request.CurrentUserId;

        // Fetch selected templates
        var templates = await _context.LabTestTemplates
            .Where(t => req.TestTemplateIds.Contains(t.Id) && t.IsActive)
            .ToListAsync(cancellationToken);

        if (!templates.Any())
            return Result<LabOrderDto>.Failure("No valid active lab tests selected");

        var countToday = await _context.LabOrders
            .CountAsync(o => o.OrderDate.Date == DateTime.UtcNow.Date, cancellationToken);
        var orderNumber = $"LAB-{DateTime.UtcNow:yyyyMMdd}-{(countToday + 1):D4}";

        Enum.TryParse<LabOrderPriority>(req.Priority, true, out var priority);

        var totalCost = templates.Sum(t => t.Price);

        var order = new LabOrder
        {
            OrderNumber = orderNumber,
            PatientId = req.PatientId,
            DoctorId = doctorId,
            AppointmentId = req.AppointmentId,
            OrderDate = DateTime.UtcNow,
            Status = LabOrderStatus.Ordered,
            Priority = priority == 0 ? LabOrderPriority.Routine : priority,
            ClinicalNotes = req.ClinicalNotes,
            TotalCost = totalCost,
            IsBilled = false
        };

        foreach (var t in templates)
        {
            order.Items.Add(new LabOrderItem
            {
                LabTestTemplateId = t.Id,
                Status = LabOrderStatus.Ordered,
                Price = t.Price
            });
        }

        _context.LabOrders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = new LabOrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            PatientId = order.PatientId,
            PatientName = $"{patient.FirstName} {patient.LastName}",
            PatientMedicalRecordNumber = patient.MedicalRecordNumber,
            DoctorId = order.DoctorId,
            OrderDate = order.OrderDate,
            Status = order.Status.ToString(),
            Priority = order.Priority.ToString(),
            ClinicalNotes = order.ClinicalNotes,
            TotalCost = order.TotalCost,
            IsBilled = order.IsBilled,
            Items = order.Items.Select(i => {
                var tmpl = templates.First(x => x.Id == i.LabTestTemplateId);
                return new LabOrderItemDto
                {
                    Id = i.Id,
                    LabOrderId = order.Id,
                    LabTestTemplateId = i.LabTestTemplateId,
                    TestCode = tmpl.TestCode,
                    TestName = tmpl.TestName,
                    Category = tmpl.Category,
                    SampleType = tmpl.SampleType,
                    Status = i.Status.ToString(),
                    Price = i.Price
                };
            }).ToList()
        };

        return Result<LabOrderDto>.Success(dto);
    }
}

public record UpdateLabOrderStatusCommand(Guid OrderId, string NewStatus) : IRequest<Result<bool>>;

public class UpdateLabOrderStatusCommandHandler : IRequestHandler<UpdateLabOrderStatusCommand, Result<bool>>
{
    private readonly IClinicDbContext _context;

    public UpdateLabOrderStatusCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(UpdateLabOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.LabOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order is null)
            return Result<bool>.Failure("Lab order not found");

        if (!Enum.TryParse<LabOrderStatus>(request.NewStatus, true, out var status))
            return Result<bool>.Failure("Invalid status value");

        order.Status = status;
        if (status == LabOrderStatus.SampleCollected)
            order.SampleCollectedAt = DateTime.UtcNow;
        else if (status == LabOrderStatus.Completed)
            order.CompletedAt = DateTime.UtcNow;

        foreach (var item in order.Items)
        {
            if (item.Status != LabOrderStatus.Completed)
                item.Status = status;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}

public record RecordLabResultCommand(RecordLabResultRequest Request, Guid TechnicianId) : IRequest<Result<LabResultDto>>;

public class RecordLabResultCommandHandler : IRequestHandler<RecordLabResultCommand, Result<LabResultDto>>
{
    private readonly IClinicDbContext _context;

    public RecordLabResultCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<LabResultDto>> Handle(RecordLabResultCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var item = await _context.LabOrderItems
            .Include(i => i.LabOrder)
            .Include(i => i.Result)
            .FirstOrDefaultAsync(i => i.Id == req.LabOrderItemId, cancellationToken);

        if (item is null)
            return Result<LabResultDto>.Failure("Lab order item not found");

        LabResult result;
        if (item.Result is null)
        {
            result = new LabResult
            {
                LabOrderItemId = item.Id,
                PerformedByTechnicianId = request.TechnicianId,
                ResultDate = DateTime.UtcNow,
                ParameterResultsJson = req.ParameterResultsJson,
                Remarks = req.Remarks,
                HasAbnormalFlag = req.HasAbnormalFlag,
                AttachmentUrl = req.AttachmentUrl
            };
            _context.LabResults.Add(result);
        }
        else
        {
            result = item.Result;
            result.PerformedByTechnicianId = request.TechnicianId;
            result.ResultDate = DateTime.UtcNow;
            result.ParameterResultsJson = req.ParameterResultsJson;
            result.Remarks = req.Remarks;
            result.HasAbnormalFlag = req.HasAbnormalFlag;
            result.AttachmentUrl = req.AttachmentUrl;
        }

        item.Status = LabOrderStatus.Completed;

        // Check if all items in order are completed
        var allItems = await _context.LabOrderItems
            .Where(x => x.LabOrderId == item.LabOrderId)
            .ToListAsync(cancellationToken);

        if (allItems.All(x => x.Id == item.Id || x.Status == LabOrderStatus.Completed))
        {
            item.LabOrder.Status = LabOrderStatus.Completed;
            item.LabOrder.CompletedAt = DateTime.UtcNow;
        }
        else
        {
            item.LabOrder.Status = LabOrderStatus.InProgress;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var dto = new LabResultDto
        {
            Id = result.Id,
            LabOrderItemId = result.LabOrderItemId,
            PerformedByTechnicianId = result.PerformedByTechnicianId,
            ResultDate = result.ResultDate,
            ParameterResultsJson = result.ParameterResultsJson,
            Remarks = result.Remarks,
            HasAbnormalFlag = result.HasAbnormalFlag,
            AttachmentUrl = result.AttachmentUrl
        };

        return Result<LabResultDto>.Success(dto);
    }
}

