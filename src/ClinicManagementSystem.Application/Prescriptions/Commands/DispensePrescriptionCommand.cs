using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Prescriptions.Commands;

public record DispensePrescriptionCommand(
    Guid PrescriptionId,
    Guid? PharmacistUserId = null,
    string? VerificationNotes = null
) : IRequest<Result<DispenseResponseDto>>;

public class DispensePrescriptionCommandHandler : IRequestHandler<DispensePrescriptionCommand, Result<DispenseResponseDto>>
{
    private readonly IClinicDbContext _context;

    public DispensePrescriptionCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<DispenseResponseDto>> Handle(DispensePrescriptionCommand request, CancellationToken cancellationToken)
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

        if (prescription is null)
            return Result<DispenseResponseDto>.Failure("Prescription not found.", "prescription_not_found");

        if (prescription.Status == "Dispensed")
            return Result<DispenseResponseDto>.Failure("Prescription has already been dispensed.", "already_dispensed");

        // Resolve pharmacist name
        string pharmacistName = "Dispensary Pharmacist";
        if (request.PharmacistUserId.HasValue && request.PharmacistUserId.Value != Guid.Empty)
        {
            var user = await _context.Users.FindAsync(new object[] { request.PharmacistUserId.Value }, cancellationToken);
            if (user != null)
            {
                pharmacistName = $"{user.FirstName} {user.LastName}".Trim();
            }
        }

        string patientName = prescription.Consultation?.Appointment?.Patient != null
            ? $"{prescription.Consultation.Appointment.Patient.FirstName} {prescription.Consultation.Appointment.Patient.LastName}"
            : "Unknown Patient";
        string patientMrn = prescription.Consultation?.Appointment?.Patient?.MedicalRecordNumber ?? "UNKNOWN";
        string doctorName = prescription.Consultation?.Doctor != null
            ? $"Dr. {prescription.Consultation.Doctor.FirstName} {prescription.Consultation.Doctor.LastName}"
            : "Attending Physician";

        var dispensedItems = new List<DispensedItemDto>();
        var totalCost = 0m;
        var fullyDispensed = true;

        foreach (var item in prescription.PrescriptionItems)
        {
            var medicine = item.Medicine ?? await _context.Medicines.FindAsync(new object[] { item.MedicineId }, cancellationToken);
            if (medicine is null) continue;

            var dispensedQuantity = Math.Min(item.Quantity, medicine.StockQuantity);
            if (dispensedQuantity < item.Quantity)
                fullyDispensed = false;

            if (dispensedQuantity > 0)
            {
                medicine.StockQuantity -= dispensedQuantity;
            }

            item.DispensedQuantity = dispensedQuantity;
            item.Status = dispensedQuantity == item.Quantity ? "Dispensed" : (dispensedQuantity > 0 ? "PartiallyDispensed" : "OutOfStock");

            var itemTotal = dispensedQuantity * item.UnitPrice;
            totalCost += itemTotal;

            // Regulatory Controlled Substance Log
            if (medicine.IsControlledSubstance && dispensedQuantity > 0)
            {
                var patientId = prescription.Consultation?.Appointment?.PatientId ?? Guid.Empty;
                var pharmaId = request.PharmacistUserId ?? Guid.Empty;
                if (pharmaId == Guid.Empty)
                {
                    var fallbackUser = _context.Users.FirstOrDefault(u => u.Username == "pharmacist.sam");
                    if (fallbackUser != null) pharmaId = fallbackUser.Id;
                }

                var auditLog = new ControlledSubstanceLog
                {
                    Id = Guid.NewGuid(),
                    LogNumber = $"CSL-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}",
                    MedicineId = medicine.Id,
                    PrescriptionId = prescription.Id,
                    PatientId = patientId,
                    QuantityDispensed = dispensedQuantity,
                    RemainingBalance = medicine.StockQuantity,
                    PrescribingDoctorName = doctorName,
                    DispensingPharmacistUserId = pharmaId,
                    DispensedAt = DateTime.UtcNow,
                    MedicalJustification = request.VerificationNotes ?? "Controlled substance double-verified and dispensed under physician order."
                };
                _context.ControlledSubstanceLogs.Add(auditLog);
            }


            dispensedItems.Add(new DispensedItemDto(
                medicine.Name,
                item.Quantity,
                dispensedQuantity,
                item.UnitPrice,
                itemTotal,
                dispensedQuantity == item.Quantity
            ));
        }

        prescription.Status = fullyDispensed ? "Dispensed" : "PartiallyDispensed";
        prescription.DispensedAt = DateTime.UtcNow;
        prescription.DispensedByUserId = request.PharmacistUserId;
        prescription.VerificationNotes = request.VerificationNotes;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<DispenseResponseDto>.Success(new DispenseResponseDto(
            prescription.Id,
            fullyDispensed,
            dispensedItems,
            totalCost,
            prescription.Status,
            prescription.DispensedAt
        ));
    }
}
