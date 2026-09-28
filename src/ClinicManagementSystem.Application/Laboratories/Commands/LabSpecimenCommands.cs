using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Laboratories.DTOs;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Laboratories.Commands;

public record IntakeSpecimenCommand(SpecimenIntakeRequest Request, Guid TechnicianUserId, string TechnicianName) : IRequest<Result<LabOrderDto>>;

public class IntakeSpecimenCommandHandler : IRequestHandler<IntakeSpecimenCommand, Result<LabOrderDto>>
{
    private readonly IClinicDbContext _context;

    public IntakeSpecimenCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<LabOrderDto>> Handle(IntakeSpecimenCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var order = await _context.LabOrders
            .Include(o => o.Patient)
            .Include(o => o.Doctor)
            .Include(o => o.Items)
                .ThenInclude(i => i.LabTestTemplate)
            .FirstOrDefaultAsync(o => o.Id == req.LabOrderId, cancellationToken);

        if (order is null)
            return Result<LabOrderDto>.Failure("Lab order not found");

        var orderSuffix = order.OrderNumber.Contains('-') ? order.OrderNumber.Split('-').Last() : Guid.NewGuid().ToString().Substring(0, 4).ToUpper();
        var barcode = $"SPM-{DateTime.UtcNow:yyyyMMdd}-{orderSuffix}";

        order.SpecimenBarcode = barcode;
        order.SpecimenType = req.SpecimenType;
        order.SampleCondition = req.SampleCondition;
        order.SampleCollectedAt = DateTime.UtcNow;
        order.CollectedByTechnicianName = request.TechnicianName;
        order.TechnicianNotes = req.TechnicianNotes;
        order.Status = LabOrderStatus.SampleCollected;
        order.IsRejected = false;
        order.RejectionReason = null;

        foreach (var item in order.Items)
        {
            item.SpecimenBarcode = barcode;
            if (item.Status == LabOrderStatus.Ordered)
            {
                item.Status = LabOrderStatus.SampleCollected;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        var dto = new LabOrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            PatientId = order.PatientId,
            PatientName = $"{order.Patient.FirstName} {order.Patient.LastName}",
            PatientMedicalRecordNumber = order.Patient.MedicalRecordNumber,
            DoctorId = order.DoctorId,
            DoctorName = $"{order.Doctor.FirstName} {order.Doctor.LastName}",
            AppointmentId = order.AppointmentId,
            OrderDate = order.OrderDate,
            Status = order.Status.ToString(),
            Priority = order.Priority.ToString(),
            ClinicalNotes = order.ClinicalNotes,
            TotalCost = order.TotalCost,
            IsBilled = order.IsBilled,
            SampleCollectedAt = order.SampleCollectedAt,
            CollectedByTechnicianName = order.CollectedByTechnicianName,
            CompletedAt = order.CompletedAt,
            SpecimenBarcode = order.SpecimenBarcode,
            SpecimenType = order.SpecimenType,
            SampleCondition = order.SampleCondition,
            IsRejected = order.IsRejected,
            RejectionReason = order.RejectionReason,
            RequiresRecollection = order.RequiresRecollection,
            TechnicianNotes = order.TechnicianNotes,
            Items = order.Items.Select(i => new LabOrderItemDto
            {
                Id = i.Id,
                LabOrderId = order.Id,
                LabTestTemplateId = i.LabTestTemplateId,
                TestCode = i.LabTestTemplate?.TestCode ?? string.Empty,
                TestName = i.LabTestTemplate?.TestName ?? string.Empty,
                Category = i.LabTestTemplate?.Category ?? string.Empty,
                SampleType = i.LabTestTemplate?.SampleType ?? string.Empty,
                Status = i.Status.ToString(),
                Price = i.Price,
                SpecimenBarcode = i.SpecimenBarcode,
                IsRejected = i.IsRejected,
                RejectionReason = i.RejectionReason
            }).ToList()
        };

        return Result<LabOrderDto>.Success(dto);
    }
}

public record RejectSpecimenCommand(SpecimenRejectRequest Request, Guid TechnicianUserId, string TechnicianName) : IRequest<Result<bool>>;

public class RejectSpecimenCommandHandler : IRequestHandler<RejectSpecimenCommand, Result<bool>>
{
    private readonly IClinicDbContext _context;

    public RejectSpecimenCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(RejectSpecimenCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var order = await _context.LabOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == req.LabOrderId, cancellationToken);

        if (order is null)
            return Result<bool>.Failure("Lab order not found");

        order.IsRejected = true;
        order.RejectionReason = req.RejectionReason;
        order.RejectedAt = DateTime.UtcNow;
        order.RejectedByTechnicianName = request.TechnicianName;
        order.RequiresRecollection = req.RequiresRecollection;
        order.TechnicianNotes = req.Notes;

        if (req.OrderItemId.HasValue)
        {
            var item = order.Items.FirstOrDefault(i => i.Id == req.OrderItemId.Value);
            if (item != null)
            {
                item.IsRejected = true;
                item.RejectionReason = req.RejectionReason;
            }
        }
        else
        {
            foreach (var item in order.Items)
            {
                item.IsRejected = true;
                item.RejectionReason = req.RejectionReason;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
