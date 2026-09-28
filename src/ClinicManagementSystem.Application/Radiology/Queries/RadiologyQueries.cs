using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Radiology.DTOs;
using ClinicManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Radiology.Queries;

public record GetRadiologyOrdersQuery(
    Guid? DoctorId = null,
    Guid? PatientId = null,
    string? Modality = null,
    string? Status = null,
    string? Priority = null,
    string? RadiologistName = null
) : IRequest<List<RadiologyOrderDto>>;

public class GetRadiologyOrdersQueryHandler : IRequestHandler<GetRadiologyOrdersQuery, List<RadiologyOrderDto>>
{
    private readonly IClinicDbContext _context;

    public GetRadiologyOrdersQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<List<RadiologyOrderDto>> Handle(GetRadiologyOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.RadiologyOrders
            .Include(r => r.Patient)
            .Include(r => r.Doctor)
            .AsNoTracking();

        if (request.DoctorId.HasValue && request.DoctorId.Value != Guid.Empty)
            query = query.Where(r => r.DoctorId == request.DoctorId.Value);

        if (request.PatientId.HasValue && request.PatientId.Value != Guid.Empty)
            query = query.Where(r => r.PatientId == request.PatientId.Value);

        if (!string.IsNullOrWhiteSpace(request.Modality) && request.Modality != "ALL")
            query = query.Where(r => r.Modality.ToLower() == request.Modality.ToLower());

        if (!string.IsNullOrWhiteSpace(request.Status) && request.Status != "ALL")
            query = query.Where(r => r.Status.ToLower() == request.Status.ToLower());

        if (!string.IsNullOrWhiteSpace(request.Priority) && request.Priority != "ALL")
            query = query.Where(r => r.Priority.ToLower() == request.Priority.ToLower());

        if (!string.IsNullOrWhiteSpace(request.RadiologistName))
        {
            var rad = request.RadiologistName.Trim().ToLower();
            query = query.Where(r =>
                (r.RadiologistSignature != null && r.RadiologistSignature.ToLower().Contains(rad)) ||
                (r.PerformedByTechnicianName != null && r.PerformedByTechnicianName.ToLower().Contains(rad))
            );
        }

        var list = await query
            .OrderByDescending(r => r.OrderedAt)
            .ToListAsync(cancellationToken);

        return list.Select(order => MapDto(order)).ToList();
    }

    internal static RadiologyOrderDto MapDto(RadiologyOrder order)
    {
        return new RadiologyOrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            AccessionNumber = order.AccessionNumber,
            RoomSuite = order.RoomSuite,
            EquipmentModel = order.EquipmentModel,
            ScheduledAt = order.ScheduledAt,
            PatientId = order.PatientId,
            PatientName = order.Patient != null ? $"{order.Patient.FirstName} {order.Patient.LastName}" : string.Empty,
            PatientMedicalRecordNumber = order.Patient?.MedicalRecordNumber ?? string.Empty,
            Gender = order.Patient?.Gender ?? string.Empty,
            DateOfBirth = order.Patient?.DateOfBirth,
            DoctorId = order.DoctorId,
            DoctorName = order.Doctor != null ? $"{order.Doctor.FirstName} {order.Doctor.LastName}" : string.Empty,
            AppointmentId = order.AppointmentId,
            Modality = order.Modality,
            ProcedureName = order.ProcedureName,
            BodyPart = order.BodyPart,
            ClinicalIndication = order.ClinicalIndication,
            Priority = order.Priority,
            Status = order.Status,
            OrderedAt = order.OrderedAt,
            PerformedAt = order.PerformedAt,
            PerformedByTechnicianName = order.PerformedByTechnicianName,
            Findings = order.Findings,
            Impression = order.Impression,
            ImageUrl = order.ImageUrl,
            SecondaryAttachmentUrls = order.SecondaryAttachmentUrls,
            RadiologistSignature = order.RadiologistSignature,
            ReportedAt = order.ReportedAt,
            IsCritical = order.IsCritical,
            CriticalAlertNotes = order.CriticalAlertNotes,
            CriticalAlertAcknowledgedByDoctor = order.CriticalAlertAcknowledgedByDoctor,
            CriticalAlertAcknowledgedAt = order.CriticalAlertAcknowledgedAt,
            RadiationDoseDlp = order.RadiationDoseDlp,
            RadiationDoseDap = order.RadiationDoseDap,
            ExposureParameters = order.ExposureParameters,
            ContrastUsed = order.ContrastUsed,
            ContrastAgentName = order.ContrastAgentName,
            ContrastVolumeMl = order.ContrastVolumeMl,
            ContrastLotNumber = order.ContrastLotNumber,
            ContrastRoute = order.ContrastRoute,
            DoctorSignedOffAt = order.DoctorSignedOffAt,
            DoctorSignOffNotes = order.DoctorSignOffNotes,
            Price = order.Price,
            IsBilled = order.IsBilled
        };
    }
}

public record GetRadiologyOrderByIdQuery(Guid Id) : IRequest<RadiologyOrderDto?>;

public class GetRadiologyOrderByIdQueryHandler : IRequestHandler<GetRadiologyOrderByIdQuery, RadiologyOrderDto?>
{
    private readonly IClinicDbContext _context;

    public GetRadiologyOrderByIdQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<RadiologyOrderDto?> Handle(GetRadiologyOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _context.RadiologyOrders
            .Include(r => r.Patient)
            .Include(r => r.Doctor)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (order == null) return null;
        return GetRadiologyOrdersQueryHandler.MapDto(order);
    }
}

public record GetRadiologyMetricsQuery(Guid? DoctorId = null, string? RadiologistName = null) : IRequest<RadiologyMetricsDto>;

public class GetRadiologyMetricsQueryHandler : IRequestHandler<GetRadiologyMetricsQuery, RadiologyMetricsDto>
{
    private readonly IClinicDbContext _context;

    public GetRadiologyMetricsQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<RadiologyMetricsDto> Handle(GetRadiologyMetricsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.RadiologyOrders.AsNoTracking();

        if (request.DoctorId.HasValue && request.DoctorId.Value != Guid.Empty)
            query = query.Where(r => r.DoctorId == request.DoctorId.Value);

        if (!string.IsNullOrWhiteSpace(request.RadiologistName))
        {
            var rad = request.RadiologistName.Trim().ToLower();
            query = query.Where(r =>
                (r.RadiologistSignature != null && r.RadiologistSignature.ToLower().Contains(rad)) ||
                (r.PerformedByTechnicianName != null && r.PerformedByTechnicianName.ToLower().Contains(rad))
            );
        }

        var total = await query.CountAsync(cancellationToken);
        var ordered = await query.CountAsync(r => r.Status == "Ordered", cancellationToken);
        var scheduled = await query.CountAsync(r => r.Status == "Scheduled", cancellationToken);
        var inProgress = await query.CountAsync(r => r.Status == "InProgress", cancellationToken);
        var completed = await query.CountAsync(r => r.Status == "Completed", cancellationToken);
        var pendingReview = await query.CountAsync(r => r.Status == "Completed" && r.DoctorSignedOffAt == null, cancellationToken);
        var statUrgent = await query.CountAsync(r => r.Priority == "Stat" || r.Priority == "Urgent", cancellationToken);
        var criticalAlerts = await query.CountAsync(r => r.IsCritical && !r.CriticalAlertAcknowledgedByDoctor, cancellationToken);

        return new RadiologyMetricsDto
        {
            TotalOrders = total,
            OrderedCount = ordered,
            ScheduledCount = scheduled,
            InProgressCount = inProgress,
            CompletedCount = completed,
            PendingDoctorReviewCount = pendingReview,
            StatUrgentCount = statUrgent,
            CriticalAlertsCount = criticalAlerts
        };
    }
}

public record GetRadiologySafetyLogsQuery(string? Modality = null) : IRequest<List<RadiologySafetyLogDto>>;

public class GetRadiologySafetyLogsQueryHandler : IRequestHandler<GetRadiologySafetyLogsQuery, List<RadiologySafetyLogDto>>
{
    private readonly IClinicDbContext _context;

    public GetRadiologySafetyLogsQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<List<RadiologySafetyLogDto>> Handle(GetRadiologySafetyLogsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.RadiologyOrders
            .Include(r => r.Patient)
            .AsNoTracking()
            .Where(r => r.ContrastUsed || r.RadiationDoseDlp != null || r.RadiationDoseDap != null || r.ExposureParameters != null || r.Status == "Completed" || r.Status == "Reported");

        if (!string.IsNullOrWhiteSpace(request.Modality) && request.Modality != "ALL")
            query = query.Where(r => r.Modality.ToLower() == request.Modality.ToLower());

        var list = await query
            .OrderByDescending(r => r.PerformedAt ?? r.ReportedAt ?? r.OrderedAt)
            .ToListAsync(cancellationToken);

        return list.Select(r => new RadiologySafetyLogDto
        {
            OrderId = r.Id,
            OrderNumber = r.OrderNumber,
            AccessionNumber = r.AccessionNumber ?? r.OrderNumber,
            PatientName = r.Patient != null ? $"{r.Patient.FirstName} {r.Patient.LastName}" : "Clinical Patient",
            Modality = r.Modality,
            ProcedureName = r.ProcedureName,
            PerformedAt = r.PerformedAt ?? r.ReportedAt ?? r.OrderedAt,
            PerformedByTechnicianName = r.PerformedByTechnicianName,
            RadiationDoseDlp = r.RadiationDoseDlp,
            RadiationDoseDap = r.RadiationDoseDap,
            ExposureParameters = r.ExposureParameters,
            ContrastUsed = r.ContrastUsed,
            ContrastAgentName = r.ContrastAgentName,
            ContrastVolumeMl = r.ContrastVolumeMl,
            ContrastLotNumber = r.ContrastLotNumber,
            ContrastRoute = r.ContrastRoute
        }).ToList();
    }
}
