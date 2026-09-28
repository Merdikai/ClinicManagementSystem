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

public record GetPrescriptionLabelQuery(Guid PrescriptionId, Guid? ItemId = null) : IRequest<List<PrescriptionLabelDto>>;

public class GetPrescriptionLabelQueryHandler : IRequestHandler<GetPrescriptionLabelQuery, List<PrescriptionLabelDto>>
{
    private readonly IClinicDbContext _context;

    public GetPrescriptionLabelQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<List<PrescriptionLabelDto>> Handle(GetPrescriptionLabelQuery request, CancellationToken cancellationToken)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.PrescriptionItems)
                .ThenInclude(pi => pi.Medicine)
            .Include(p => p.Consultation)
                .ThenInclude(c => c.Appointment)
                    .ThenInclude(a => a.Patient)
            .Include(p => p.Consultation)
                .ThenInclude(c => c.Doctor)
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, cancellationToken);

        if (prescription == null) return new List<PrescriptionLabelDto>();

        var patient = prescription.Consultation?.Appointment?.Patient;
        var patientName = patient != null ? $"{patient.FirstName} {patient.LastName}".Trim() : "Patient";
        var patientMrn = patient?.MedicalRecordNumber ?? "MRN-N/A";
        var doctorName = prescription.Consultation?.Doctor != null
            ? $"Dr. {prescription.Consultation.Doctor.FirstName} {prescription.Consultation.Doctor.LastName}".Trim()
            : "Attending Physician";

        string pharmacistName = "Dispensary Pharmacist";
        if (prescription.DispensedByUserId.HasValue)
        {
            var pharmaUser = await _context.Users.FindAsync(new object[] { prescription.DispensedByUserId.Value }, cancellationToken);
            if (pharmaUser != null)
            {
                pharmacistName = $"{pharmaUser.FirstName} {pharmaUser.LastName}".Trim();
            }
        }

        var dispensedDate = prescription.DispensedAt.HasValue
            ? prescription.DispensedAt.Value.ToString("yyyy-MM-dd HH:mm")
            : DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");

        var items = prescription.PrescriptionItems.AsEnumerable();
        if (request.ItemId.HasValue)
        {
            items = items.Where(i => i.Id == request.ItemId.Value);
        }

        var labels = new List<PrescriptionLabelDto>();
        int itemIndex = 1;

        foreach (var item in items)
        {
            var med = item.Medicine;
            var medName = med?.Name ?? "Prescribed Medication";
            var genericName = med?.GenericName ?? "";
            var strength = med?.Strength ?? "";
            var form = med?.DosageForm ?? "Tablet";
            var instructions = !string.IsNullOrWhiteSpace(item.DosageInstructions)
                ? item.DosageInstructions
                : "Take as directed by doctor.";

            var label = new PrescriptionLabelDto
            {
                PrescriptionId = prescription.Id,
                RxNumber = $"RX-{prescription.Id.ToString()[..8].ToUpper()}-{itemIndex:D2}",
                PatientName = patientName,
                PatientMrn = patientMrn,
                MedicineName = medName,
                GenericName = genericName,
                Strength = strength,
                DosageForm = form,
                Quantity = item.Quantity,
                DosageInstructions = instructions,
                DoctorName = doctorName,
                DispensedDate = dispensedDate,
                PharmacistName = pharmacistName,
                ClinicName = "MedFlow Health Clinic — Central Dispensary",
                ClinicPhone = "+1 (555) 019-2834",
                CautionNotes = med != null && med.IsControlledSubstance
                    ? "CAUTION: Controlled Substance. Transfer to any person other than the patient for whom prescribed is prohibited by law."
                    : "Keep out of reach of children. Store in a cool, dry place. Finish all medication unless instructed otherwise."
            };

            labels.Add(label);
            itemIndex++;
        }

        return labels;
    }
}
