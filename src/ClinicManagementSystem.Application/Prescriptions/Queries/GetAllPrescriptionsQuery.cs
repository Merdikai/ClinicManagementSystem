using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Prescriptions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Prescriptions.Queries;

public record GetAllPrescriptionsQuery(Guid? PatientId = null, string? Status = null) : IRequest<List<PrescriptionDto>>;

public class GetAllPrescriptionsQueryHandler : IRequestHandler<GetAllPrescriptionsQuery, List<PrescriptionDto>>
{
    private readonly IClinicDbContext _context;

    public GetAllPrescriptionsQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<List<PrescriptionDto>> Handle(GetAllPrescriptionsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Prescriptions
            .AsNoTracking()
            .AsQueryable();

        if (request.PatientId.HasValue && request.PatientId.Value != Guid.Empty)
        {
            query = query.Where(p => p.Consultation != null && 
                                     p.Consultation.Appointment != null && 
                                     p.Consultation.Appointment.PatientId == request.PatientId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(p => p.Status.ToLower() == request.Status.ToLower());
        }

        return await query
            .OrderByDescending(p => p.IssuedAt)
            .Select(p => new PrescriptionDto
            {
                Id = p.Id,
                ConsultationId = p.ConsultationId,
                PatientId = p.Consultation != null && p.Consultation.Appointment != null ? p.Consultation.Appointment.PatientId : null,
                PatientName = p.Consultation != null && p.Consultation.Appointment != null && p.Consultation.Appointment.Patient != null
                    ? p.Consultation.Appointment.Patient.FirstName + " " + p.Consultation.Appointment.Patient.LastName
                    : "Patient",
                PatientMrn = p.Consultation != null && p.Consultation.Appointment != null && p.Consultation.Appointment.Patient != null
                    ? p.Consultation.Appointment.Patient.MedicalRecordNumber
                    : "",
                PatientAllergies = p.Consultation != null && p.Consultation.Appointment != null && p.Consultation.Appointment.Patient != null
                    ? p.Consultation.Appointment.Patient.Allergies
                    : null,
                DoctorId = p.Consultation != null ? p.Consultation.DoctorId : (Guid?)null,
                DoctorName = p.Consultation != null && p.Consultation.Doctor != null
                    ? "Dr. " + p.Consultation.Doctor.FirstName + " " + p.Consultation.Doctor.LastName
                    : "Attending Physician",
                Notes = p.Notes,
                Status = p.Status ?? "Pending",
                IssuedAt = p.IssuedAt,
                DispensedAt = p.DispensedAt,
                DispensedByUserId = p.DispensedByUserId,
                DispensedByPharmacistName = p.DispensedByUser != null
                    ? p.DispensedByUser.FirstName + " " + p.DispensedByUser.LastName
                    : null,
                VerificationNotes = p.VerificationNotes,
                InvoiceId = p.InvoiceId,
                InvoiceNumber = p.Invoice != null ? p.Invoice.InvoiceNumber : null,
                TotalAmount = p.PrescriptionItems.Sum(pi => pi.Quantity * pi.UnitPrice),
                Items = p.PrescriptionItems.Select(pi => new PrescriptionItemDto
                {
                    Id = pi.Id,
                    MedicineId = pi.MedicineId,
                    MedicineName = pi.Medicine != null ? pi.Medicine.Name : "Medicine",
                    GenericName = pi.Medicine != null ? pi.Medicine.GenericName : null,
                    Strength = pi.Medicine != null ? pi.Medicine.Strength : null,
                    DosageForm = pi.Medicine != null ? pi.Medicine.DosageForm : null,
                    Category = pi.Medicine != null ? pi.Medicine.Category : null,
                    Quantity = pi.Quantity,
                    DispensedQuantity = pi.DispensedQuantity,
                    AvailableStock = pi.Medicine != null ? pi.Medicine.StockQuantity : 0,
                    IsControlledSubstance = pi.Medicine != null && pi.Medicine.IsControlledSubstance,
                    Status = pi.Status ?? "Pending",
                    DosageInstructions = pi.DosageInstructions,
                    UnitPrice = pi.UnitPrice,
                    TotalPrice = pi.TotalPrice
                }).ToList()
            })
            .ToListAsync(cancellationToken);
    }
}

