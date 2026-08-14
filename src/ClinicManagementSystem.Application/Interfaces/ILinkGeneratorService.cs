using ClinicManagementSystem.Application.DTOs;

namespace ClinicManagementSystem.Application.Interfaces;

public interface ILinkGeneratorService
{
    List<LinkDto> GeneratePatientLinks(Guid patientId);
    List<LinkDto> GenerateAppointmentLinks(ClinicManagementSystem.Domain.Entities.Appointment appointment);
    List<LinkDto> GenerateInvoiceLinks(Guid invoiceId);
}
