using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Radiology.DTOs;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Radiology.Commands;

public record CreateRadiologyOrderCommand(
    Guid PatientId,
    Guid DoctorId,
    Guid? AppointmentId,
    string Modality,
    string ProcedureName,
    string BodyPart,
    string ClinicalIndication,
    string Priority,
    decimal Price,
    string? RoomSuite = null
) : IRequest<RadiologyOrderDto>;

public class CreateRadiologyOrderCommandHandler : IRequestHandler<CreateRadiologyOrderCommand, RadiologyOrderDto>
{
    private readonly IClinicDbContext _context;

    public CreateRadiologyOrderCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<RadiologyOrderDto> Handle(CreateRadiologyOrderCommand request, CancellationToken cancellationToken)
    {
        var patient = await _context.Patients.FindAsync(new object[] { request.PatientId }, cancellationToken);
        if (patient == null)
            throw new KeyNotFoundException($"Patient with ID '{request.PatientId}' was not found.");

        var doctor = await _context.Users.FindAsync(new object[] { request.DoctorId }, cancellationToken);
        if (doctor == null)
            throw new KeyNotFoundException($"Doctor with ID '{request.DoctorId}' was not found.");

        var countToday = await _context.RadiologyOrders
            .CountAsync(r => r.OrderedAt.Date == DateTime.UtcNow.Date, cancellationToken);
        var orderNumber = $"RAD-{DateTime.UtcNow:yyyyMMdd}-{(countToday + 1):D4}";
        var accessionNumber = $"ACC-{DateTime.UtcNow:yyyyMMdd}-{(countToday + 1):D4}";

        var defaultSuite = request.RoomSuite;
        if (string.IsNullOrWhiteSpace(defaultSuite))
        {
            defaultSuite = request.Modality.ToLower() switch
            {
                "x-ray" => "Suite 1 - Digital X-Ray",
                "ct scan" => "Suite 2 - Siemens SOMATOM CT",
                "mri" => "Suite 3 - GE SIGNA 1.5T MRI",
                "ultrasound" => "Suite 4 - Ultrasound Bay A",
                _ => "Suite 1 - General Imaging"
            };
        }

        var order = new RadiologyOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = orderNumber,
            AccessionNumber = accessionNumber,
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            AppointmentId = request.AppointmentId,
            Modality = string.IsNullOrWhiteSpace(request.Modality) ? "X-Ray" : request.Modality.Trim(),
            ProcedureName = request.ProcedureName.Trim(),
            BodyPart = request.BodyPart?.Trim() ?? string.Empty,
            ClinicalIndication = request.ClinicalIndication?.Trim() ?? string.Empty,
            Priority = string.IsNullOrWhiteSpace(request.Priority) ? "Routine" : request.Priority.Trim(),
            Status = "Ordered",
            RoomSuite = defaultSuite,
            OrderedAt = DateTime.UtcNow,
            Price = request.Price > 0 ? request.Price : 75.00m,
            IsBilled = false
        };

        _context.RadiologyOrders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(order, patient, doctor);
    }

    private static RadiologyOrderDto MapToDto(RadiologyOrder order, Patient patient, User doctor)
    {
        return new RadiologyOrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            AccessionNumber = order.AccessionNumber,
            RoomSuite = order.RoomSuite,
            EquipmentModel = order.EquipmentModel,
            ScheduledAt = order.ScheduledAt,
            PatientId = patient.Id,
            PatientName = $"{patient.FirstName} {patient.LastName}",
            PatientMedicalRecordNumber = patient.MedicalRecordNumber,
            Gender = patient.Gender,
            DateOfBirth = patient.DateOfBirth,
            DoctorId = doctor.Id,
            DoctorName = $"{doctor.FirstName} {doctor.LastName}",
            AppointmentId = order.AppointmentId,
            Modality = order.Modality,
            ProcedureName = order.ProcedureName,
            BodyPart = order.BodyPart,
            ClinicalIndication = order.ClinicalIndication,
            Priority = order.Priority,
            Status = order.Status,
            OrderedAt = order.OrderedAt,
            Price = order.Price,
            IsBilled = order.IsBilled
        };
    }
}

public record ScheduleRadiologyOrderCommand(
    Guid OrderId,
    string RoomSuite,
    string? EquipmentModel,
    DateTime? ScheduledAt
) : IRequest<RadiologyOrderDto>;

public class ScheduleRadiologyOrderCommandHandler : IRequestHandler<ScheduleRadiologyOrderCommand, RadiologyOrderDto>
{
    private readonly IClinicDbContext _context;

    public ScheduleRadiologyOrderCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<RadiologyOrderDto> Handle(ScheduleRadiologyOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.RadiologyOrders
            .Include(r => r.Patient)
            .Include(r => r.Doctor)
            .FirstOrDefaultAsync(r => r.Id == request.OrderId, cancellationToken);

        if (order == null)
            throw new KeyNotFoundException($"Radiology order with ID '{request.OrderId}' was not found.");

        order.RoomSuite = request.RoomSuite.Trim();
        if (!string.IsNullOrWhiteSpace(request.EquipmentModel))
            order.EquipmentModel = request.EquipmentModel.Trim();
        order.ScheduledAt = request.ScheduledAt ?? DateTime.UtcNow;
        if (order.Status == "Ordered")
            order.Status = "Scheduled";

        await _context.SaveChangesAsync(cancellationToken);
        return MapToFullDto(order);
    }

    private static RadiologyOrderDto MapToFullDto(RadiologyOrder order) => RadiologyOrderMapper.Map(order);
}

public record ReportRadiologyOrderCommand(
    Guid OrderId,
    string TechnicianOrRadiologistName,
    string Findings,
    string Impression,
    string? ImageUrl,
    string? RadiologistSignature,
    bool IsCritical = false,
    string? CriticalAlertNotes = null,
    string? SecondaryAttachmentUrls = null,
    decimal? RadiationDoseDlp = null,
    decimal? RadiationDoseDap = null,
    string? ExposureParameters = null,
    bool ContrastUsed = false,
    string? ContrastAgentName = null,
    decimal? ContrastVolumeMl = null,
    string? ContrastLotNumber = null,
    string? ContrastRoute = null
) : IRequest<RadiologyOrderDto>;

public class ReportRadiologyOrderCommandHandler : IRequestHandler<ReportRadiologyOrderCommand, RadiologyOrderDto>
{
    private readonly IClinicDbContext _context;

    public ReportRadiologyOrderCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<RadiologyOrderDto> Handle(ReportRadiologyOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.RadiologyOrders
            .Include(r => r.Patient)
            .Include(r => r.Doctor)
            .FirstOrDefaultAsync(r => r.Id == request.OrderId, cancellationToken);

        if (order == null)
            throw new KeyNotFoundException($"Radiology order with ID '{request.OrderId}' was not found.");

        var newSig = string.IsNullOrWhiteSpace(request.RadiologistSignature)
            ? (string.IsNullOrWhiteSpace(request.TechnicianOrRadiologistName) ? "Attending Radiologist, MD" : $"Dr. {request.TechnicianOrRadiologistName}, MD")
            : request.RadiologistSignature.Trim();

        var reportingTech = string.IsNullOrWhiteSpace(request.TechnicianOrRadiologistName)
            ? "Radiologist" : request.TechnicianOrRadiologistName.Trim();

        // Radiologist Account Isolation & Medical-Legal Immutability Guard:
        // If the study was already reported & signed by another radiologist, format modifications as an official Addendum.
        var isPreviouslySigned = order.Status == "Completed" && !string.IsNullOrWhiteSpace(order.RadiologistSignature);
        var isDifferentRadiologist = isPreviouslySigned &&
            !order.RadiologistSignature!.Contains(reportingTech, StringComparison.OrdinalIgnoreCase) &&
            !newSig.Equals(order.RadiologistSignature, StringComparison.OrdinalIgnoreCase);

        if (isDifferentRadiologist)
        {
            var addendumHeader = $"\n\n--- AMENDMENT / ADDENDUM ({DateTime.UtcNow:yyyy-MM-dd HH:mm UTC} by {newSig}) ---\n";
            order.Findings = $"{order.Findings}{addendumHeader}{request.Findings.Trim()}";
            order.Impression = $"{order.Impression}{addendumHeader}{request.Impression.Trim()}";
            order.RadiologistSignature = $"{order.RadiologistSignature} | Addendum: {newSig}";
            order.DoctorSignOffNotes = (order.DoctorSignOffNotes + $" | Amended by {newSig}").Trim(' ', '|');
        }
        else
        {
            order.Findings = request.Findings.Trim();
            order.Impression = request.Impression.Trim();
            order.RadiologistSignature = newSig;
            order.PerformedByTechnicianName = reportingTech;
        }

        order.Status = "Completed";
        order.PerformedAt ??= DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.ImageUrl))
            order.ImageUrl = request.ImageUrl.Trim();
        if (!string.IsNullOrWhiteSpace(request.SecondaryAttachmentUrls))
            order.SecondaryAttachmentUrls = request.SecondaryAttachmentUrls.Trim();
        order.ReportedAt = DateTime.UtcNow;

        // Critical Alert
        order.IsCritical = request.IsCritical;
        order.CriticalAlertNotes = request.CriticalAlertNotes?.Trim();
        if (request.IsCritical)
        {
            order.CriticalAlertAcknowledgedByDoctor = false;
        }

        // Radiation & Contrast
        if (request.RadiationDoseDlp.HasValue) order.RadiationDoseDlp = request.RadiationDoseDlp;
        if (request.RadiationDoseDap.HasValue) order.RadiationDoseDap = request.RadiationDoseDap;
        if (!string.IsNullOrWhiteSpace(request.ExposureParameters)) order.ExposureParameters = request.ExposureParameters.Trim();
        order.ContrastUsed = request.ContrastUsed;
        if (!string.IsNullOrWhiteSpace(request.ContrastAgentName)) order.ContrastAgentName = request.ContrastAgentName.Trim();
        if (request.ContrastVolumeMl.HasValue) order.ContrastVolumeMl = request.ContrastVolumeMl;
        if (!string.IsNullOrWhiteSpace(request.ContrastLotNumber)) order.ContrastLotNumber = request.ContrastLotNumber.Trim();
        if (!string.IsNullOrWhiteSpace(request.ContrastRoute)) order.ContrastRoute = request.ContrastRoute.Trim();

        await _context.SaveChangesAsync(cancellationToken);
        return RadiologyOrderMapper.Map(order);
    }
}

public record AcknowledgeCriticalAlertCommand(
    Guid OrderId,
    Guid DoctorId,
    string? Notes = null
) : IRequest<RadiologyOrderDto>;

public class AcknowledgeCriticalAlertCommandHandler : IRequestHandler<AcknowledgeCriticalAlertCommand, RadiologyOrderDto>
{
    private readonly IClinicDbContext _context;

    public AcknowledgeCriticalAlertCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<RadiologyOrderDto> Handle(AcknowledgeCriticalAlertCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.RadiologyOrders
            .Include(r => r.Patient)
            .Include(r => r.Doctor)
            .FirstOrDefaultAsync(r => r.Id == request.OrderId, cancellationToken);

        if (order == null)
            throw new KeyNotFoundException($"Radiology order with ID '{request.OrderId}' was not found.");

        order.CriticalAlertAcknowledgedByDoctor = true;
        order.CriticalAlertAcknowledgedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            order.DoctorSignOffNotes = (order.DoctorSignOffNotes + " | Critical alert ack: " + request.Notes).Trim(' ', '|');
        }

        await _context.SaveChangesAsync(cancellationToken);
        return RadiologyOrderMapper.Map(order);
    }
}

public record DoctorSignOffRadiologyCommand(
    Guid OrderId,
    Guid DoctorId,
    string? Notes,
    bool IsAdmin = false
) : IRequest<RadiologyOrderDto>;

public class DoctorSignOffRadiologyCommandHandler : IRequestHandler<DoctorSignOffRadiologyCommand, RadiologyOrderDto>
{
    private readonly IClinicDbContext _context;

    public DoctorSignOffRadiologyCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<RadiologyOrderDto> Handle(DoctorSignOffRadiologyCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.RadiologyOrders
            .Include(r => r.Patient)
            .Include(r => r.Doctor)
            .FirstOrDefaultAsync(r => r.Id == request.OrderId, cancellationToken);

        if (order == null)
            throw new KeyNotFoundException($"Radiology order with ID '{request.OrderId}' was not found.");

        // Doctor Account Isolation Enforcement
        if (order.DoctorId != Guid.Empty && order.DoctorId != request.DoctorId && !request.IsAdmin)
        {
            var attendingName = order.Doctor != null
                ? $"Dr. {order.Doctor.FirstName} {order.Doctor.LastName}".Trim()
                : "the assigned physician";
            throw new UnauthorizedAccessException($"Doctor Account Isolation: You are not the ordering physician for this radiology order. Only attending physician {attendingName} can sign off.");
        }

        order.DoctorSignedOffAt = DateTime.UtcNow;
        order.DoctorSignOffNotes = string.IsNullOrWhiteSpace(request.Notes)
            ? "Reviewed and clinically acknowledged by attending physician." : request.Notes.Trim();

        if (order.IsCritical)
        {
            order.CriticalAlertAcknowledgedByDoctor = true;
            order.CriticalAlertAcknowledgedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return RadiologyOrderMapper.Map(order);
    }
}

public record BillRadiologyOrderCommand(Guid OrderId) : IRequest<RadiologyOrderDto>;

public class BillRadiologyOrderCommandHandler : IRequestHandler<BillRadiologyOrderCommand, RadiologyOrderDto>
{
    private readonly IClinicDbContext _context;

    public BillRadiologyOrderCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<RadiologyOrderDto> Handle(BillRadiologyOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.RadiologyOrders
            .Include(r => r.Patient)
            .Include(r => r.Doctor)
            .FirstOrDefaultAsync(r => r.Id == request.OrderId, cancellationToken);

        if (order == null)
            throw new KeyNotFoundException($"Radiology order with ID '{request.OrderId}' was not found.");

        order.IsBilled = true;

        // Synchronize with Billing System & Accounting Role:
        // Ensure an Invoice and InvoiceItem exist for this diagnostic imaging order
        var existingInvoice = order.AppointmentId.HasValue
            ? await _context.Invoices
                .Include(i => i.LineItems)
                .FirstOrDefaultAsync(i => i.AppointmentId == order.AppointmentId && i.Status == InvoiceStatus.Unpaid, cancellationToken)
            : await _context.Invoices
                .Include(i => i.LineItems)
                .FirstOrDefaultAsync(i => i.PatientId == order.PatientId && i.Status == InvoiceStatus.Unpaid, cancellationToken);

        var itemDescription = $"Diagnostic Imaging: {order.Modality} - {order.ProcedureName} ({order.AccessionNumber})";

        if (existingInvoice != null)
        {
            var alreadyInvoiced = existingInvoice.LineItems.Any(li =>
                li.Description.Contains(order.AccessionNumber) ||
                li.Description.Contains(order.OrderNumber));

            if (!alreadyInvoiced)
            {
                existingInvoice.LineItems.Add(new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = existingInvoice.Id,
                    Description = itemDescription,
                    Quantity = 1,
                    UnitPrice = order.Price
                });
                existingInvoice.SubTotal = existingInvoice.LineItems.Sum(li => li.Quantity * li.UnitPrice);
                existingInvoice.TotalAmount = Math.Max(0, existingInvoice.SubTotal + existingInvoice.TaxAmount - existingInvoice.DiscountAmount);
            }
        }
        else
        {
            var newInvoice = new Invoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
                PatientId = order.PatientId,
                AppointmentId = order.AppointmentId,
                Department = "Radiology",
                Status = InvoiceStatus.Unpaid,
                IssueDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(7),
                SubTotal = order.Price,
                TotalAmount = order.Price,
                BillingNotes = $"Generated from RIS for {order.Modality} order {order.OrderNumber}"
            };
            newInvoice.LineItems.Add(new InvoiceItem
            {
                Id = Guid.NewGuid(),
                InvoiceId = newInvoice.Id,
                Description = itemDescription,
                Quantity = 1,
                UnitPrice = order.Price
            });
            _context.Invoices.Add(newInvoice);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return RadiologyOrderMapper.Map(order);
    }
}

public record UpdateRadiologyOrderStatusCommand(
    Guid OrderId,
    string Status,
    string? Notes = null
) : IRequest<RadiologyOrderDto>;

public class UpdateRadiologyOrderStatusCommandHandler : IRequestHandler<UpdateRadiologyOrderStatusCommand, RadiologyOrderDto>
{
    private readonly IClinicDbContext _context;

    public UpdateRadiologyOrderStatusCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<RadiologyOrderDto> Handle(UpdateRadiologyOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.RadiologyOrders
            .Include(r => r.Patient)
            .Include(r => r.Doctor)
            .FirstOrDefaultAsync(r => r.Id == request.OrderId, cancellationToken);

        if (order == null)
            throw new KeyNotFoundException($"Radiology order with ID '{request.OrderId}' was not found.");

        order.Status = request.Status.Trim();
        if (order.Status == "InProgress" || order.Status == "In Scan")
        {
            order.PerformedAt ??= DateTime.UtcNow;
        }

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            order.ClinicalIndication = (order.ClinicalIndication + $" | Status Note: {request.Notes}").Trim(' ', '|');
        }

        await _context.SaveChangesAsync(cancellationToken);
        return RadiologyOrderMapper.Map(order);
    }
}

public record CreateRadiologySafetyEntryCommand(
    string? AccessionNumber,
    string? PatientName,
    Guid? PatientId,
    string Modality,
    string ProcedureName,
    string? PerformedByTechnicianName,
    decimal? RadiationDoseDlp,
    decimal? RadiationDoseDap,
    string? ExposureParameters,
    bool ContrastUsed,
    string? ContrastAgentName,
    decimal? ContrastVolumeMl,
    string? ContrastLotNumber,
    string? ContrastRoute
) : IRequest<RadiologySafetyLogDto>;

public class CreateRadiologySafetyEntryCommandHandler : IRequestHandler<CreateRadiologySafetyEntryCommand, RadiologySafetyLogDto>
{
    private readonly IClinicDbContext _context;

    public CreateRadiologySafetyEntryCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<RadiologySafetyLogDto> Handle(CreateRadiologySafetyEntryCommand request, CancellationToken cancellationToken)
    {
        RadiologyOrder? order = null;

        if (!string.IsNullOrWhiteSpace(request.AccessionNumber))
        {
            order = await _context.RadiologyOrders
                .Include(r => r.Patient)
                .FirstOrDefaultAsync(r => r.AccessionNumber == request.AccessionNumber, cancellationToken);
        }

        if (order != null)
        {
            // Update existing order with dosimetry / contrast data
            order.RadiationDoseDlp = request.RadiationDoseDlp;
            order.RadiationDoseDap = request.RadiationDoseDap;
            order.ExposureParameters = request.ExposureParameters?.Trim();
            order.ContrastUsed = request.ContrastUsed;
            order.ContrastAgentName = request.ContrastAgentName?.Trim();
            order.ContrastVolumeMl = request.ContrastVolumeMl;
            order.ContrastLotNumber = request.ContrastLotNumber?.Trim();
            order.ContrastRoute = request.ContrastRoute?.Trim();
            if (!string.IsNullOrWhiteSpace(request.PerformedByTechnicianName))
                order.PerformedByTechnicianName = request.PerformedByTechnicianName.Trim();
            order.PerformedAt ??= DateTime.UtcNow;
            if (order.Status == "Ordered" || order.Status == "Scheduled" || order.Status == "InProgress")
                order.Status = "Completed";
        }
        else
        {
            // Determine Patient
            Patient? patient = null;
            if (request.PatientId.HasValue && request.PatientId.Value != Guid.Empty)
            {
                patient = await _context.Patients.FindAsync(new object[] { request.PatientId.Value }, cancellationToken);
            }
            if (patient == null && !string.IsNullOrWhiteSpace(request.PatientName))
            {
                patient = await _context.Patients
                    .FirstOrDefaultAsync(p => (p.FirstName + " " + p.LastName).ToLower() == request.PatientName.Trim().ToLower(), cancellationToken);
            }
            if (patient == null)
            {
                patient = await _context.Patients.FirstOrDefaultAsync(cancellationToken);
                if (patient == null)
                    throw new InvalidOperationException("No patient record exists to associate with this radiology safety audit entry.");
            }

            // Determine Doctor
            var doctor = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserRoles.Any(ur => ur.Role.Name == "Doctor" || ur.Role.Name == "Radiologist"), cancellationToken)
                ?? await _context.Users.FirstOrDefaultAsync(cancellationToken);

            if (doctor == null)
                throw new InvalidOperationException("No physician user found in system.");

            var countToday = await _context.RadiologyOrders
                .CountAsync(r => r.OrderedAt.Date == DateTime.UtcNow.Date, cancellationToken);
            var orderNum = $"RAD-{DateTime.UtcNow:yyyyMMdd}-{(countToday + 1):D4}";
            var accNum = !string.IsNullOrWhiteSpace(request.AccessionNumber)
                ? request.AccessionNumber.Trim()
                : $"ACC-{DateTime.UtcNow:yyyyMMdd}-{(countToday + 1):D4}";

            order = new RadiologyOrder
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                Modality = string.IsNullOrWhiteSpace(request.Modality) ? "CT Scan" : request.Modality.Trim(),
                ProcedureName = string.IsNullOrWhiteSpace(request.ProcedureName) ? "Radiology Examination" : request.ProcedureName.Trim(),
                BodyPart = "Clinical Region",
                ClinicalIndication = "Safety & Dosimetry Audit Log",
                Priority = "Routine",
                Status = "Completed",
                OrderNumber = orderNum,
                AccessionNumber = accNum,
                OrderedAt = DateTime.UtcNow,
                PerformedAt = DateTime.UtcNow,
                ReportedAt = DateTime.UtcNow,
                PerformedByTechnicianName = request.PerformedByTechnicianName ?? "Attending Radiologist",
                Findings = "Procedure completed. Radiation dosimetry and contrast safety compliance parameters recorded.",
                Impression = "Completed and compliant with hospital radiation safety regulations.",
                RadiologistSignature = request.PerformedByTechnicianName ?? "Attending Radiologist, MD",
                RadiationDoseDlp = request.RadiationDoseDlp,
                RadiationDoseDap = request.RadiationDoseDap,
                ExposureParameters = request.ExposureParameters?.Trim(),
                ContrastUsed = request.ContrastUsed,
                ContrastAgentName = request.ContrastAgentName?.Trim(),
                ContrastVolumeMl = request.ContrastVolumeMl,
                ContrastLotNumber = request.ContrastLotNumber?.Trim(),
                ContrastRoute = request.ContrastRoute?.Trim()
            };

            _context.RadiologyOrders.Add(order);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var patientName = order.Patient != null
            ? $"{order.Patient.FirstName} {order.Patient.LastName}"
            : (request.PatientName ?? "Clinical Patient");

        return new RadiologySafetyLogDto
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            AccessionNumber = order.AccessionNumber,
            PatientName = patientName,
            Modality = order.Modality,
            ProcedureName = order.ProcedureName,
            PerformedAt = order.PerformedAt ?? order.OrderedAt,
            PerformedByTechnicianName = order.PerformedByTechnicianName,
            RadiationDoseDlp = order.RadiationDoseDlp,
            RadiationDoseDap = order.RadiationDoseDap,
            ExposureParameters = order.ExposureParameters,
            ContrastUsed = order.ContrastUsed,
            ContrastAgentName = order.ContrastAgentName,
            ContrastVolumeMl = order.ContrastVolumeMl,
            ContrastLotNumber = order.ContrastLotNumber,
            ContrastRoute = order.ContrastRoute
        };
    }
}

internal static class RadiologyOrderMapper
{
    public static RadiologyOrderDto Map(RadiologyOrder order)
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
