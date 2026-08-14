using ClinicManagementSystem.Application.DTOs;

namespace ClinicManagementSystem.Application.Interfaces;

public interface ILinkGeneratorService
{
    List<LinkDto> GeneratePatientLinks(Guid patientId);
    List<LinkDto> GenerateAppointmentLinks(Guid appointmentId);
    List<LinkDto> GenerateInvoiceLinks(Guid invoiceId);
}
