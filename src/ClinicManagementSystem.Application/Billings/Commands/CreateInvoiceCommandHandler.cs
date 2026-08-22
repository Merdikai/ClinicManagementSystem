using AutoMapper;
using ClinicManagementSystem.Application.Billings.Commands;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Billings.Commands;

public class CreateInvoiceCommandHandler : IRequestHandler<CreateInvoiceCommand, Result<InvoiceResponseDto>>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IMapper _mapper;
    private readonly ILinkGeneratorService _linkGenerator;

    public CreateInvoiceCommandHandler(IInvoiceRepository invoiceRepository, IMapper mapper, ILinkGeneratorService linkGenerator)
    {
        _invoiceRepository = invoiceRepository;
        _mapper = mapper;
        _linkGenerator = linkGenerator;
    }

    public async Task<Result<InvoiceResponseDto>> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = new Invoice
        {
            InvoiceNumber = GenerateInvoiceNumber(),
            PatientId = request.PatientId,
            AppointmentId = request.AppointmentId,
            DueDate = DateTime.UtcNow.AddDays(30),
            Status = InvoiceStatus.Unpaid,
            TaxAmount = request.TaxAmount,
            DiscountAmount = request.DiscountAmount
        };

        foreach (var itemDto in request.Items)
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
        await _invoiceRepository.SaveChangesAsync();

        var dto = _mapper.Map<InvoiceResponseDto>(invoice);
        dto.Links = _linkGenerator.GenerateInvoiceLinks(invoice.Id);
        return Result<InvoiceResponseDto>.Success(dto);
    }

    private static string GenerateInvoiceNumber()
    {
        var year = DateTime.UtcNow.Year;
        var random = new Random().Next(1000, 9999);
        return $"INV-{year}-{random}";
    }
}
