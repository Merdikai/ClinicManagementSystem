using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Medicines.Commands;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using Xunit;

namespace ClinicManagementSystem.UnitTests.Application.Medicines;

#pragma warning disable EXTEXP0018
public class MedicineCommandHandlerTests
{
    private readonly IMedicineRepository _medicineRepository = Substitute.For<IMedicineRepository>();
    private readonly IInvoiceRepository _invoiceRepository = Substitute.For<IInvoiceRepository>();
    private readonly IPaymentRepository _paymentRepository = Substitute.For<IPaymentRepository>();
    private readonly IPatientRepository _patientRepository = Substitute.For<IPatientRepository>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();

    [Fact]
    public async Task BulkRestock_ShouldCallRepositoryAndInvalidateCache()
    {
        var handler = new BulkRestockCommandHandler(_medicineRepository, _cache);
        var updates = new Dictionary<Guid, int> { { Guid.NewGuid(), 50 } };

        await handler.Handle(new BulkRestockCommand(updates), CancellationToken.None);

        await _medicineRepository.Received(1).BulkRestockAsync(updates);
        await _cache.Received(1).RemoveByTagAsync("medicines", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BulkUpdatePrices_ShouldCallRepositoryAndInvalidateCache()
    {
        var handler = new BulkUpdatePricesCommandHandler(_medicineRepository, _cache);
        var updates = new Dictionary<Guid, decimal> { { Guid.NewGuid(), 19.99m } };

        await handler.Handle(new BulkUpdatePricesCommand(updates), CancellationToken.None);

        await _medicineRepository.Received(1).BulkUpdatePricesAsync(updates);
        await _cache.Received(1).RemoveByTagAsync("medicines", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispenseMedicine_InsufficientStock_ShouldThrowBusinessRuleViolation()
    {
        var medId = Guid.NewGuid();
        var medicine = new Medicine
        {
            Id = medId,
            Name = "Paracetamol",
            StockQuantity = 5,
            UnitPrice = 2.00m
        };

        _medicineRepository.GetByIdAsync(medId).Returns(medicine);

        var handler = new DispenseMedicineCommandHandler(
            _medicineRepository, _invoiceRepository, _paymentRepository,
            _patientRepository, _cache, _notificationService);

        var act = async () => await handler.Handle(new DispenseMedicineCommand(medId, 10), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("*Insufficient stock*");
    }

    [Fact]
    public async Task DispenseMedicine_NotFound_ShouldThrowNotFoundException()
    {
        var medId = Guid.NewGuid();
        _medicineRepository.GetByIdAsync(medId).Returns((Medicine?)null);

        var handler = new DispenseMedicineCommandHandler(
            _medicineRepository, _invoiceRepository, _paymentRepository,
            _patientRepository, _cache, _notificationService);

        var act = async () => await handler.Handle(new DispenseMedicineCommand(medId, 1), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
#pragma warning restore EXTEXP0018

