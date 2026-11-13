using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace ClinicManagementSystem.Application.Prescriptions.Commands;

public record DispensePrescriptionCommand(
    Guid PrescriptionId,
    Guid? PharmacistUserId = null,
    string? VerificationNotes = null
) : IRequest<Result<DispenseResponseDto>>;

#pragma warning disable EXTEXP0018
public class DispensePrescriptionCommandHandler : IRequestHandler<DispensePrescriptionCommand, Result<DispenseResponseDto>>
{
    private readonly IClinicDbContext _context;
    private readonly ICurrentUserService? _currentUserService;
    private readonly HybridCache? _cache;

    public DispensePrescriptionCommandHandler(
        IClinicDbContext context,
        ICurrentUserService? currentUserService = null,
        HybridCache? cache = null)
    {
        _context = context;
        _currentUserService = currentUserService;
        _cache = cache;
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

        var resolvedPharmacistId = request.PharmacistUserId ?? _currentUserService?.UserId;

        string doctorName = prescription.Consultation?.Doctor != null
            ? $"Dr. {prescription.Consultation.Doctor.FirstName} {prescription.Consultation.Doctor.LastName}"
            : "Attending Physician";

        var dispensedItems = new List<DispensedItemDto>();
        var totalCost = 0m;
        var fullyDispensed = true;

        foreach (var item in prescription.PrescriptionItems)
        {
            var medicine = item.Medicine ?? await _context.Medicines.FindAsync([item.MedicineId], cancellationToken);
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

            // Regulatory Controlled Substance Log (must use verified dispensing pharmacist identity)
            if (medicine.IsControlledSubstance && dispensedQuantity > 0)
            {
                if (!resolvedPharmacistId.HasValue || resolvedPharmacistId.Value == Guid.Empty)
                {
                    return Result<DispenseResponseDto>.Failure(
                        "Authenticated dispensing pharmacist identity is required to log controlled substances.",
                        "controlled_substance_pharmacist_required");
                }

                var patientId = prescription.Consultation?.Appointment?.PatientId ?? Guid.Empty;
                var auditLog = new ControlledSubstanceLog
                {
                    Id = Guid.NewGuid(),
                    LogNumber = $"CSL-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
                    MedicineId = medicine.Id,
                    PrescriptionId = prescription.Id,
                    PatientId = patientId,
                    QuantityDispensed = dispensedQuantity,
                    RemainingBalance = medicine.StockQuantity,
                    PrescribingDoctorName = doctorName,
                    DispensingPharmacistUserId = resolvedPharmacistId.Value,
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
        prescription.DispensedByUserId = resolvedPharmacistId;
        prescription.VerificationNotes = request.VerificationNotes;

        await _context.SaveChangesAsync(cancellationToken);

        if (_cache is not null)
        {
            try { await _cache.RemoveByTagAsync("medicines", cancellationToken); } catch { }
        }

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
#pragma warning restore EXTEXP0018
