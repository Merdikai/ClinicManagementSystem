using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Radiology.DTOs;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Consultations.Commands;

public record CreateConsultationCommand(
    Guid AppointmentId,
    Guid DoctorId,
    string Symptoms,
    string Diagnosis,
    string Findings,
    string ClinicalNotes,
    string? PrescriptionNotes = null,
    List<CreatePrescriptionItemDto>? PrescriptionItems = null,
    decimal? SystolicBP = null,
    decimal? DiastolicBP = null,
    decimal? TemperatureC = null,
    decimal? HeartRateBpm = null,
    decimal? RespiratoryRate = null,
    decimal? WeightKg = null,
    decimal? HeightCm = null,
    List<Guid>? LabTestTemplateIds = null,
    string? LabPriority = null,
    string? LabClinicalNotes = null,
    List<CreateRadiologyOrderDto>? RadiologyOrders = null,
    List<CreateNursingProcedureOrderDto>? NursingProcedureOrders = null,
    bool IsAdmin = false
) : IRequest<ConsultationResponseDto>;

public class CreateConsultationCommandHandler : IRequestHandler<CreateConsultationCommand, ConsultationResponseDto>
{
    private readonly IClinicDbContext _context;

    public CreateConsultationCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<ConsultationResponseDto> Handle(CreateConsultationCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken);

        if (appointment == null)
            throw new KeyNotFoundException($"Appointment with ID '{request.AppointmentId}' was not found.");

        // Doctor Account Isolation Enforcement:
        // A doctor can only conduct consultations on appointments explicitly assigned to their account,
        // unless an active doctor delegation is in effect where request.DoctorId is covering for the attending physician.
        if (appointment.DoctorId != Guid.Empty && appointment.DoctorId != request.DoctorId && !request.IsAdmin)
        {
            var now = DateTime.UtcNow;
            var hasActiveDelegation = await _context.DoctorDelegations.AnyAsync(d =>
                d.IsActive &&
                d.OriginalDoctorId == appointment.DoctorId &&
                d.CoveringDoctorId == request.DoctorId &&
                d.StartDateUtc <= now &&
                d.EndDateUtc >= now, cancellationToken);

            if (!hasActiveDelegation)
            {
                var attendingName = appointment.Doctor != null 
                    ? $"Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName}".Trim() 
                    : "the assigned physician";
                throw new UnauthorizedAccessException($"Doctor Account Isolation: You are not the assigned physician or an authorized covering delegate for this appointment. Only attending physician {attendingName} or an active covering physician can conduct consultations for this patient.");
            }
        }

        var consultation = new Consultation
        {
            Id = Guid.NewGuid(),
            AppointmentId = request.AppointmentId,
            DoctorId = request.DoctorId,
            Symptoms = request.Symptoms ?? string.Empty,
            Diagnosis = request.Diagnosis ?? string.Empty,
            Findings = request.Findings ?? string.Empty,
            ClinicalNotes = request.ClinicalNotes ?? string.Empty,
            ConsultedAt = DateTime.UtcNow
        };

        // 1. Record session vitals if provided
        if (request.SystolicBP.HasValue || request.TemperatureC.HasValue || request.HeartRateBpm.HasValue)
        {
            var vitalSign = new VitalSign
            {
                Id = Guid.NewGuid(),
                AppointmentId = request.AppointmentId,
                RecordedByUserId = request.DoctorId,
                ConsultationId = consultation.Id,
                SystolicBP = request.SystolicBP ?? 120m,
                DiastolicBP = request.DiastolicBP ?? 80m,
                TemperatureC = request.TemperatureC ?? 36.8m,
                HeartRateBpm = request.HeartRateBpm ?? 72m,
                RespiratoryRate = request.RespiratoryRate ?? 16m,
                WeightKg = request.WeightKg ?? 70m,
                HeightCm = request.HeightCm ?? 170m,
                RecordedAt = DateTime.UtcNow
            };
            _context.VitalSigns.Add(vitalSign);
        }

        // 2. Issue prescription if items are attached
        if (request.PrescriptionItems != null && request.PrescriptionItems.Count > 0)
        {
            var prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                ConsultationId = consultation.Id,
                Notes = request.PrescriptionNotes ?? string.Empty,
                IssuedAt = DateTime.UtcNow
            };

            foreach (var item in request.PrescriptionItems)
            {
                var med = await _context.Medicines.FindAsync(new object[] { item.MedicineId }, cancellationToken);
                var unitPrice = med?.UnitPrice ?? 0m;
                prescription.PrescriptionItems.Add(new PrescriptionItem
                {
                    Id = Guid.NewGuid(),
                    PrescriptionId = prescription.Id,
                    MedicineId = item.MedicineId,
                    Quantity = item.Quantity,
                    DosageInstructions = item.DosageInstructions ?? string.Empty,
                    UnitPrice = unitPrice
                });
            }
            consultation.Prescription = prescription;
            _context.Prescriptions.Add(prescription);
        }

        // 3. Dispatch Diagnostic Lab Orders if templates were selected
        Guid? createdLabOrderId = null;
        if (request.LabTestTemplateIds != null && request.LabTestTemplateIds.Count > 0)
        {
            var templates = await _context.LabTestTemplates
                .Where(t => request.LabTestTemplateIds.Contains(t.Id))
                .ToListAsync(cancellationToken);

            if (templates.Count > 0)
            {
                var countToday = await _context.LabOrders
                    .CountAsync(o => o.OrderDate.Date == DateTime.UtcNow.Date, cancellationToken);
                var orderNumber = $"LAB-{DateTime.UtcNow:yyyyMMdd}-{(countToday + 1):D4}";

                var priorityEnum = LabOrderPriority.Routine;
                if (!string.IsNullOrWhiteSpace(request.LabPriority))
                {
                    Enum.TryParse(request.LabPriority, true, out priorityEnum);
                }

                var labOrder = new LabOrder
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = orderNumber,
                    PatientId = appointment.PatientId,
                    DoctorId = request.DoctorId,
                    AppointmentId = appointment.Id,
                    OrderDate = DateTime.UtcNow,
                    Status = LabOrderStatus.Ordered,
                    Priority = priorityEnum,
                    ClinicalNotes = string.IsNullOrWhiteSpace(request.LabClinicalNotes)
                        ? $"Consultation order for {consultation.Diagnosis}"
                        : request.LabClinicalNotes.Trim(),
                    TotalCost = templates.Sum(t => t.Price),
                    IsBilled = false
                };

                foreach (var tmpl in templates)
                {
                    labOrder.Items.Add(new LabOrderItem
                    {
                        Id = Guid.NewGuid(),
                        LabOrderId = labOrder.Id,
                        LabTestTemplateId = tmpl.Id,
                        Status = LabOrderStatus.Ordered,
                        Price = tmpl.Price
                    });
                }

                _context.LabOrders.Add(labOrder);
                createdLabOrderId = labOrder.Id;
            }
        }

        // 4. Dispatch Radiology / Diagnostic Imaging Orders
        var createdRadiologyIds = new List<Guid>();
        var createdRadiologyOrders = new List<RadiologyOrder>();
        if (request.RadiologyOrders != null && request.RadiologyOrders.Count > 0)
        {
            var radCount = await _context.RadiologyOrders
                .CountAsync(r => r.OrderedAt.Date == DateTime.UtcNow.Date, cancellationToken);

            foreach (var radReq in request.RadiologyOrders)
            {
                radCount++;
                var modality = string.IsNullOrWhiteSpace(radReq.Modality) ? "X-Ray" : radReq.Modality.Trim();
                var defaultSuite = string.IsNullOrWhiteSpace(radReq.RoomSuite)
                    ? (modality.ToLower() switch
                    {
                        "x-ray" => "Suite 1 - Digital X-Ray",
                        "ct scan" => "Suite 2 - Siemens SOMATOM CT",
                        "mri" => "Suite 3 - GE SIGNA 1.5T MRI",
                        "ultrasound" => "Suite 4 - Ultrasound Bay A",
                        "ecg / echo" => "Suite 5 - Echocardiography & Vascular Lab",
                        _ => "Suite 1 - General Imaging"
                    })
                    : radReq.RoomSuite.Trim();

                var radOrder = new RadiologyOrder
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = $"RAD-{DateTime.UtcNow:yyyyMMdd}-{radCount:D4}",
                    AccessionNumber = $"ACC-{DateTime.UtcNow:yyyyMMdd}-{radCount:D4}",
                    PatientId = appointment.PatientId,
                    DoctorId = request.DoctorId,
                    AppointmentId = appointment.Id,
                    Modality = modality,
                    ProcedureName = radReq.ProcedureName.Trim(),
                    BodyPart = radReq.BodyPart?.Trim() ?? string.Empty,
                    ClinicalIndication = string.IsNullOrWhiteSpace(radReq.ClinicalIndication)
                        ? $"Clinical suspicion: {consultation.Diagnosis}" : radReq.ClinicalIndication.Trim(),
                    Priority = string.IsNullOrWhiteSpace(radReq.Priority) ? "Routine" : radReq.Priority.Trim(),
                    Status = "Ordered",
                    RoomSuite = defaultSuite,
                    OrderedAt = DateTime.UtcNow,
                    Price = radReq.Price > 0 ? radReq.Price : 75.00m,
                    IsBilled = true
                };

                _context.RadiologyOrders.Add(radOrder);
                createdRadiologyOrders.Add(radOrder);
                createdRadiologyIds.Add(radOrder.Id);
            }
        }

        // 5. Dispatch In-Clinic Nursing Procedure Orders
        var createdNursingIds = new List<Guid>();
        if (request.NursingProcedureOrders != null && request.NursingProcedureOrders.Count > 0)
        {
            foreach (var procReq in request.NursingProcedureOrders)
            {
                var proc = new NursingProcedure
                {
                    Id = Guid.NewGuid(),
                    PatientId = appointment.PatientId,
                    AppointmentId = appointment.Id,
                    OrderedByDoctorId = request.DoctorId,
                    ProcedureType = procReq.ProcedureType.Trim(),
                    BodySite = procReq.BodySite?.Trim() ?? string.Empty,
                    Notes = procReq.Notes?.Trim() ?? string.Empty,
                    Priority = string.IsNullOrWhiteSpace(procReq.Priority) ? "Routine" : procReq.Priority.Trim(),
                    Status = "Ordered",
                    Price = procReq.Price > 0 ? procReq.Price : 30.00m,
                    IsBilled = false,
                    OrderedAt = DateTime.UtcNow,
                    PerformedAt = null
                };

                _context.NursingProcedures.Add(proc);
                createdNursingIds.Add(proc.Id);
            }
        }

        // 6. Automatically generate / append to billing invoice
        var invoice = await _context.Invoices
            .Include(inv => inv.LineItems)
            .FirstOrDefaultAsync(inv => inv.AppointmentId == appointment.Id, cancellationToken);

        if (invoice == null)
        {
            invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
                PatientId = appointment.PatientId,
                AppointmentId = appointment.Id,
                SubTotal = 50.00m,
                TotalAmount = 50.00m, // Consultation standard fee
                Status = InvoiceStatus.Unpaid,
                Department = "Consultation",
                IssueDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(7)
            };
            invoice.LineItems.Add(new InvoiceItem
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                Description = $"Medical Consultation with Attending Physician ({consultation.Diagnosis})",
                Quantity = 1,
                UnitPrice = 50.00m
            });
            _context.Invoices.Add(invoice);
        }

        // Append diagnostic imaging / radiology orders to the invoice line items
        if (createdRadiologyOrders.Count > 0)
        {
            foreach (var rad in createdRadiologyOrders)
            {
                invoice.LineItems.Add(new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    Description = $"Diagnostic Imaging: {rad.Modality} - {rad.ProcedureName} ({rad.AccessionNumber})",
                    Quantity = 1,
                    UnitPrice = rad.Price
                });
            }
            invoice.SubTotal = invoice.LineItems.Sum(i => i.Quantity * i.UnitPrice);
            invoice.TotalAmount = invoice.SubTotal;
        }

        appointment.Status = AppointmentStatus.Completed;
        _context.Consultations.Add(consultation);

        await _context.SaveChangesAsync(cancellationToken);

        var doctor = await _context.Users.FindAsync(new object[] { consultation.DoctorId }, cancellationToken);
        var doctorName = doctor != null ? $"{doctor.FirstName} {doctor.LastName}" : "Doctor";

        return new ConsultationResponseDto
        {
            Id = consultation.Id,
            AppointmentId = consultation.AppointmentId,
            DoctorName = doctorName,
            Symptoms = consultation.Symptoms,
            Diagnosis = consultation.Diagnosis,
            Findings = consultation.Findings,
            ClinicalNotes = consultation.ClinicalNotes,
            ConsultedAt = consultation.ConsultedAt,
            Prescription = consultation.Prescription != null ? new PrescriptionResponseDto
            {
                Id = consultation.Prescription.Id,
                ConsultationId = consultation.Prescription.ConsultationId,
                IssuedAt = consultation.Prescription.IssuedAt,
                Notes = consultation.Prescription.Notes,
                Items = consultation.Prescription.PrescriptionItems.Select(i => new PrescriptionItemResponseDto
                {
                    Id = i.Id,
                    MedicineId = i.MedicineId,
                    Quantity = i.Quantity,
                    DosageInstructions = i.DosageInstructions,
                    UnitPrice = i.UnitPrice
                }).ToList()
            } : null,
            LabOrderId = createdLabOrderId,
            RadiologyOrderIds = createdRadiologyIds,
            NursingProcedureIds = createdNursingIds
        };
    }
}
