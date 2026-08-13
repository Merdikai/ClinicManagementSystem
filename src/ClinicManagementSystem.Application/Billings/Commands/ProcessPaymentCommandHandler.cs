using AutoMapper;
using ClinicManagementSystem.Application.Billings.Commands;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Billings.Commands;

public class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand, PaymentResponseDto>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IMapper _mapper;

    public ProcessPaymentCommandHandler(IInvoiceRepository invoiceRepository, IPaymentRepository paymentRepository, IMapper mapper)
    {
        _invoiceRepository = invoiceRepository;
        _paymentRepository = paymentRepository;
        _mapper = mapper;
    }

    public async Task<PaymentResponseDto> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId)
            ?? throw new NotFoundException(nameof(Invoice), request.InvoiceId);

        var payment = new Payment
        {
            InvoiceId = request.InvoiceId,
            AmountPaid = request.AmountPaid,
            PaymentMethod = request.PaymentMethod,
            TransactionReference = request.TransactionReference
        };

        await _paymentRepository.AddAsync(payment);

        var balance = invoice.BalanceDue;
        if (balance <= 0)
            invoice.Status = InvoiceStatus.Paid;
        else if (invoice.Payments.Any())
            invoice.Status = InvoiceStatus.PartiallyPaid;

        _invoiceRepository.Update(invoice);
        await _paymentRepository.SaveChangesAsync();
        await _invoiceRepository.SaveChangesAsync();

        return _mapper.Map<PaymentResponseDto>(payment);
    }
}
