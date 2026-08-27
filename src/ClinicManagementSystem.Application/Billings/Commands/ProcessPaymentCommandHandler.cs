using AutoMapper;
using ClinicManagementSystem.Application.Billings.Commands;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using ClinicManagementSystem.Domain.Interfaces;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Billings.Commands;

public class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand, Result<PaymentResponseDto>>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;

    public ProcessPaymentCommandHandler(IInvoiceRepository invoiceRepository, IPaymentRepository paymentRepository, IMapper mapper, INotificationService notificationService)
    {
        _invoiceRepository = invoiceRepository;
        _paymentRepository = paymentRepository;
        _mapper = mapper;
        _notificationService = notificationService;
    }

    public async Task<Result<PaymentResponseDto>> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId);
        if (invoice is null)
            return Result<PaymentResponseDto>.Failure($"Invoice {request.InvoiceId} not found", "invoice_not_found");

        var refCode = !string.IsNullOrWhiteSpace(request.TransactionReference)
            ? request.TransactionReference
            : $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        var payment = new Payment
        {
            InvoiceId = request.InvoiceId,
            AmountPaid = request.AmountPaid,
            PaymentMethod = !string.IsNullOrWhiteSpace(request.PaymentMethod) ? request.PaymentMethod : "Cash",
            TransactionReference = refCode
        };

        await _paymentRepository.AddAsync(payment);
        invoice.Payments.Add(payment);

        var balance = invoice.BalanceDue;
        if (balance <= 0)
            invoice.Status = InvoiceStatus.Paid;
        else if (invoice.Payments.Any())
            invoice.Status = InvoiceStatus.PartiallyPaid;

        _invoiceRepository.Update(invoice);
        await _paymentRepository.SaveChangesAsync();
        await _invoiceRepository.SaveChangesAsync();

        await _notificationService.NotifyInvoicePaidAsync(invoice.PatientId, request.InvoiceId, request.AmountPaid);

        return Result<PaymentResponseDto>.Success(_mapper.Map<PaymentResponseDto>(payment));
    }
}
