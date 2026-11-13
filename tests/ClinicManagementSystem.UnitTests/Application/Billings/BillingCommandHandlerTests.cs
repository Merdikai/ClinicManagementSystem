using AutoMapper;
using ClinicManagementSystem.Application.Billings.Commands;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using ClinicManagementSystem.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace ClinicManagementSystem.UnitTests.Application.Billings;

public class BillingCommandHandlerTests
{
    private readonly IInvoiceRepository _invoiceRepository = Substitute.For<IInvoiceRepository>();
    private readonly IPaymentRepository _paymentRepository = Substitute.For<IPaymentRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly ILinkGeneratorService _linkGenerator = Substitute.For<ILinkGeneratorService>();
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();

    [Fact]
    public async Task CreateInvoice_ShouldCalculateTotalsAndAddInvoice()
    {
        var patientId = Guid.NewGuid();
        var command = new CreateInvoiceCommand(
            PatientId: patientId,
            AppointmentId: null,
            Items: new List<CreateInvoiceItemDto>
            {
                new("Consultation Fee", 1, 100.00m),
                new("Blood Test", 2, 25.00m)
            },
            TaxAmount: 15.00m,
            DiscountAmount: 10.00m
        );

        _mapper.Map<InvoiceResponseDto>(Arg.Any<Invoice>()).Returns(new InvoiceResponseDto
        {
            Id = Guid.NewGuid(),
            TotalAmount = 155.00m,
            Status = InvoiceStatus.Unpaid.ToString()
        });

        var handler = new CreateInvoiceCommandHandler(_invoiceRepository, _mapper, _linkGenerator);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _invoiceRepository.Received(1).AddAsync(Arg.Is<Invoice>(inv =>
            inv.SubTotal == 150.00m &&
            inv.TotalAmount == 155.00m &&
            inv.Status == InvoiceStatus.Unpaid
        ));
    }

    [Fact]
    public async Task ProcessPayment_FullPayment_ShouldSetInvoiceStatusToPaid()
    {
        var invoiceId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var invoice = new Invoice
        {
            Id = invoiceId,
            PatientId = patientId,
            TotalAmount = 100.00m,
            Status = InvoiceStatus.Unpaid,
            Payments = new List<Payment>()
        };

        _invoiceRepository.GetByIdAsync(invoiceId).Returns(invoice);
        _mapper.Map<PaymentResponseDto>(Arg.Any<Payment>()).Returns(new PaymentResponseDto
        {
            Id = Guid.NewGuid(),
            AmountPaid = 100.00m
        });

        var handler = new ProcessPaymentCommandHandler(_invoiceRepository, _paymentRepository, _mapper, _notificationService);
        var command = new ProcessPaymentCommand(invoiceId, 100.00m, "CreditCard", "REF-999");
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        await _paymentRepository.Received(1).AddAsync(Arg.Any<Payment>());
        await _notificationService.Received(1).NotifyInvoicePaidAsync(patientId, invoiceId, 100.00m);
    }

    [Fact]
    public async Task ProcessPayment_PartialPayment_ShouldSetInvoiceStatusToPartiallyPaid()
    {
        var invoiceId = Guid.NewGuid();
        var invoice = new Invoice
        {
            Id = invoiceId,
            PatientId = Guid.NewGuid(),
            TotalAmount = 200.00m,
            Status = InvoiceStatus.Unpaid,
            Payments = new List<Payment>()
        };

        _invoiceRepository.GetByIdAsync(invoiceId).Returns(invoice);
        _mapper.Map<PaymentResponseDto>(Arg.Any<Payment>()).Returns(new PaymentResponseDto
        {
            Id = Guid.NewGuid(),
            AmountPaid = 50.00m
        });

        var handler = new ProcessPaymentCommandHandler(_invoiceRepository, _paymentRepository, _mapper, _notificationService);
        var command = new ProcessPaymentCommand(invoiceId, 50.00m, "Cash", "REF-PARTIAL");
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        invoice.Status.Should().Be(InvoiceStatus.PartiallyPaid);
    }

    [Fact]
    public async Task ProcessPayment_InvoiceNotFound_ShouldReturnFailure()
    {
        var invoiceId = Guid.NewGuid();
        _invoiceRepository.GetByIdAsync(invoiceId).Returns((Invoice?)null);

        var handler = new ProcessPaymentCommandHandler(_invoiceRepository, _paymentRepository, _mapper, _notificationService);
        var result = await handler.Handle(new ProcessPaymentCommand(invoiceId, 50.00m, "Cash", "REF-0"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("invoice_not_found");
    }
}

