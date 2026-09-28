using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Laboratories.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Laboratories.Commands;

public record DoctorSignOffLabOrderCommand(
    Guid OrderId,
    Guid DoctorId,
    string? Notes,
    bool IsAdmin = false
) : IRequest<LabOrderDto>;

public class DoctorSignOffLabOrderCommandHandler : IRequestHandler<DoctorSignOffLabOrderCommand, LabOrderDto>
{
    private readonly IClinicDbContext _context;

    public DoctorSignOffLabOrderCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<LabOrderDto> Handle(DoctorSignOffLabOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.LabOrders
            .Include(o => o.Patient)
            .Include(o => o.Doctor)
            .Include(o => o.Items)
                .ThenInclude(i => i.LabTestTemplate)
            .Include(o => o.Items)
                .ThenInclude(i => i.Result)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
            throw new KeyNotFoundException($"Lab order with ID '{request.OrderId}' was not found.");

        // Doctor Account Isolation Enforcement:
        // Only the ordering / attending physician can sign off on this diagnostic lab order.
        if (order.DoctorId != Guid.Empty && order.DoctorId != request.DoctorId && !request.IsAdmin)
        {
            var attendingName = order.Doctor != null
                ? $"Dr. {order.Doctor.FirstName} {order.Doctor.LastName}".Trim()
                : "the assigned physician";
            throw new UnauthorizedAccessException($"Doctor Account Isolation: You are not the ordering physician for this lab order. Only attending physician {attendingName} can sign off.");
        }

        // Set VerifiedByDoctorId on all child results
        foreach (var item in order.Items)
        {
            if (item.Result != null)
            {
                item.Result.VerifiedByDoctorId = request.DoctorId;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            var signOffTag = $"[Doctor Sign-Off by Dr. {order.Doctor?.LastName ?? "Attending"} on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC: {request.Notes.Trim()}]";
            order.TechnicianNotes = string.IsNullOrWhiteSpace(order.TechnicianNotes)
                ? signOffTag : $"{order.TechnicianNotes}\n{signOffTag}";
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new LabOrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            PatientId = order.PatientId,
            PatientName = order.Patient != null ? $"{order.Patient.FirstName} {order.Patient.LastName}" : string.Empty,
            PatientMedicalRecordNumber = order.Patient?.MedicalRecordNumber ?? string.Empty,
            DoctorId = order.DoctorId,
            DoctorName = order.Doctor != null ? $"{order.Doctor.FirstName} {order.Doctor.LastName}" : "Attending Doctor",
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
            RejectedAt = order.RejectedAt,
            RejectedByTechnicianName = order.RejectedByTechnicianName,
            RequiresRecollection = order.RequiresRecollection,
            TechnicianNotes = order.TechnicianNotes,
            Items = order.Items.Select(i => new LabOrderItemDto
            {
                Id = i.Id,
                LabTestTemplateId = i.LabTestTemplateId,
                TestCode = i.LabTestTemplate?.TestCode ?? string.Empty,
                TestName = i.LabTestTemplate?.TestName ?? string.Empty,
                Category = i.LabTestTemplate?.Category ?? string.Empty,
                SampleType = i.LabTestTemplate?.SampleType ?? string.Empty,
                Status = i.Status.ToString(),
                Price = i.Price,
                SpecimenBarcode = i.SpecimenBarcode,
                IsRejected = i.IsRejected,
                RejectionReason = i.RejectionReason,
                Result = i.Result != null ? new LabResultDto
                {
                    Id = i.Result.Id,
                    LabOrderItemId = i.Result.LabOrderItemId,
                    PerformedByTechnicianId = i.Result.PerformedByTechnicianId,
                    ResultDate = i.Result.ResultDate,
                    ParameterResultsJson = i.Result.ParameterResultsJson,
                    Remarks = i.Result.Remarks,
                    HasAbnormalFlag = i.Result.HasAbnormalFlag,
                    AttachmentUrl = i.Result.AttachmentUrl
                } : null
            }).ToList()
        };
    }
}
