using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using ClinicManagementSystem.Application.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ClinicManagementSystem.Application.Medicines.Commands;

#pragma warning disable EXTEXP0018
public class DispenseMedicineCommandHandler : IRequestHandler<DispenseMedicineCommand>
{
    private readonly IMedicineRepository _medicineRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly HybridCache _cache;
    private readonly INotificationService _notificationService;

    public DispenseMedicineCommandHandler(
        IMedicineRepository medicineRepository, 
        IInvoiceRepository invoiceRepository,
        IPaymentRepository paymentRepository,
        IPatientRepository patientRepository,
        HybridCache cache, 
        INotificationService notificationService)
    {
        _medicineRepository = medicineRepository;
        _invoiceRepository = invoiceRepository;
        _paymentRepository = paymentRepository;
        _patientRepository = patientRepository;
        _cache = cache;
        _notificationService = notificationService;
    }

    public async Task Handle(DispenseMedicineCommand request, CancellationToken cancellationToken)
    {
        var medicine = await _medicineRepository.GetByIdAsync(request.MedicineId)
            ?? throw new NotFoundException(nameof(Medicine), request.MedicineId);

        if (medicine.StockQuantity < request.Quantity)
            throw new BusinessRuleViolationException(
                $"Insufficient stock. Available: {medicine.StockQuantity}, Requested: {request.Quantity}",
                "insufficient_stock");

        medicine.StockQuantity -= request.Quantity;
        _medicineRepository.Update(medicine);
        
        // Find or create walk-in patient
        var patients = await _patientRepository.GetAllAsync();
        var walkInPatient = patients.FirstOrDefault(p => p.FirstName == "Walk-in" && p.LastName == "Patient");
        
        if (walkInPatient == null)
        {
            walkInPatient = new Patient
            {
                Id = Guid.NewGuid(),
                FirstName = "Walk-in",
                LastName = "Patient",
                DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Gender = "Unknown",
                MedicalRecordNumber = $"WALKIN-{new Random().Next(100000, 999999)}"
            };
            await _patientRepository.AddAsync(walkInPatient);
            // Save immediately so it can be referenced by the Invoice
            await _patientRepository.SaveChangesAsync();
        }

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = $"OTC-{DateTime.UtcNow.Year}-{new Random().Next(1000, 9999)}",
            PatientId = walkInPatient.Id,
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow,
            Status = InvoiceStatus.Paid,
            TaxAmount = 0,
            DiscountAmount = 0,
            SubTotal = medicine.UnitPrice * request.Quantity,
            TotalAmount = medicine.UnitPrice * request.Quantity
        };

        invoice.LineItems.Add(new InvoiceItem
        {
            Description = $"{medicine.Name} (OTC Sale)",
            Quantity = request.Quantity,
            UnitPrice = medicine.UnitPrice
        });

        await _invoiceRepository.AddAsync(invoice);

        var payment = new Payment
        {
            InvoiceId = invoice.Id,
            AmountPaid = invoice.TotalAmount,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = "Cash",
            TransactionReference = $"CSH-{new Random().Next(100000, 999999)}"
        };
        await _paymentRepository.AddAsync(payment);

        await _medicineRepository.SaveChangesAsync();

        if (medicine.StockQuantity < 10)
        {
            await _notificationService.NotifyLowStockAsync(medicine.Name, medicine.StockQuantity);
        }

        await _cache.RemoveByTagAsync("medicines", cancellationToken);
    }
}
#pragma warning restore EXTEXP0018
