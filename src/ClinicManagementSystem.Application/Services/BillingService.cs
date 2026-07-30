using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using ClinicManagementSystem.Domain.Interfaces;

namespace ClinicManagementSystem.Application.Services;

public class BillingService : IBillingService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IMapper _mapper;

    public BillingService(IInvoiceRepository invoiceRepository, IPaymentRepository paymentRepository, IMapper mapper)
    {
        _invoiceRepository = invoiceRepository;
        _paymentRepository = paymentRepository;
        _mapper = mapper;
    }

    public async Task<InvoiceResponseDto> CreateInvoiceAsync(CreateInvoiceDto dto)
    {
        var invoice = new Invoice
        {
            InvoiceNumber = GenerateInvoiceNumber(),
            PatientId = dto.PatientId,
            AppointmentId = dto.AppointmentId,
            DueDate = DateTime.UtcNow.AddDays(30),
            Status = InvoiceStatus.Unpaid,
            TaxAmount = dto.TaxAmount,
            DiscountAmount = dto.DiscountAmount
        };

        foreach (var itemDto in dto.Items)
        {
            invoice.LineItems.Add(new InvoiceItem
            {
                Description = itemDto.Description,
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice
            });
        }

        invoice.SubTotal = invoice.LineItems.Sum(i => i.LineTotal);
        invoice.TotalAmount = invoice.SubTotal + invoice.TaxAmount - invoice.DiscountAmount;

        await _invoiceRepository.AddAsync(invoice);
        return _mapper.Map<InvoiceResponseDto>(invoice);
    }

    public async Task<InvoiceResponseDto?> GetInvoiceByIdAsync(Guid id)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(id);
        if (invoice is null) throw new NotFoundException(nameof(Invoice), id);
        return _mapper.Map<InvoiceResponseDto>(invoice);
    }

    public async Task<IEnumerable<InvoiceResponseDto>> GetInvoicesByPatientAsync(Guid patientId)
    {
        var invoices = await _invoiceRepository.GetByPatientIdAsync(patientId);
        return _mapper.Map<IEnumerable<InvoiceResponseDto>>(invoices);
    }

    public async Task<PaymentResponseDto> ProcessPaymentAsync(ProcessPaymentDto dto)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(dto.InvoiceId);
        if (invoice is null) throw new NotFoundException(nameof(Invoice), dto.InvoiceId);

        var payment = new Payment
        {
            InvoiceId = dto.InvoiceId,
            AmountPaid = dto.AmountPaid,
            PaymentMethod = dto.PaymentMethod,
            TransactionReference = dto.TransactionReference
        };

        await _paymentRepository.AddAsync(payment);

        // Update invoice status
        var balance = invoice.BalanceDue;
        if (balance <= 0)
            invoice.Status = InvoiceStatus.Paid;
        else if (invoice.Payments.Any())
            invoice.Status = InvoiceStatus.PartiallyPaid;

        _invoiceRepository.Update(invoice);

        return _mapper.Map<PaymentResponseDto>(payment);
    }

    private string GenerateInvoiceNumber()
    {
        var year = DateTime.UtcNow.Year;
        var random = new Random().Next(1000, 9999);
        return $"INV-{year}-{random}";
    }
}