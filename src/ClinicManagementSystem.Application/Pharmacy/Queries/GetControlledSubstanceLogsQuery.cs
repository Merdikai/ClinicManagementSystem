using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Pharmacy.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Pharmacy.Queries;

public record GetControlledSubstanceLogsQuery() : IRequest<List<ControlledSubstanceLogDto>>;

public class GetControlledSubstanceLogsQueryHandler : IRequestHandler<GetControlledSubstanceLogsQuery, List<ControlledSubstanceLogDto>>
{
    private readonly IClinicDbContext _context;

    public GetControlledSubstanceLogsQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<List<ControlledSubstanceLogDto>> Handle(GetControlledSubstanceLogsQuery request, CancellationToken cancellationToken)
    {
        return await _context.ControlledSubstanceLogs
            .Include(l => l.Medicine)
            .Include(l => l.Patient)
            .Include(l => l.DispensingPharmacist)
            .OrderByDescending(l => l.DispensedAt)
            .Select(l => new ControlledSubstanceLogDto
            {
                Id = l.Id,
                LogNumber = l.LogNumber,
                MedicineId = l.MedicineId,
                MedicineName = l.Medicine != null ? l.Medicine.Name : "Controlled Substance",
                Strength = l.Medicine != null ? l.Medicine.Strength : null,
                DosageForm = l.Medicine != null ? l.Medicine.DosageForm : null,
                PrescriptionId = l.PrescriptionId,
                PatientId = l.PatientId,
                PatientName = l.Patient != null ? $"{l.Patient.FirstName} {l.Patient.LastName}".Trim() : "Patient",
                PatientMrn = l.Patient != null ? l.Patient.MedicalRecordNumber : "",
                PrescribingDoctorName = l.PrescribingDoctorName,
                PharmacistName = l.DispensingPharmacist != null
                    ? $"{l.DispensingPharmacist.FirstName} {l.DispensingPharmacist.LastName}".Trim()
                    : "Pharmacist",
                WitnessPharmacistName = l.WitnessPharmacistName,
                QuantityDispensed = l.QuantityDispensed,
                RemainingBalance = l.RemainingBalance,
                MedicalJustification = l.MedicalJustification,
                DispensedAt = l.DispensedAt
            })
            .ToListAsync(cancellationToken);
    }
}

