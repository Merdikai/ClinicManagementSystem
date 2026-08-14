using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;

namespace ClinicManagementSystem.Application.Services;

public class LinkGeneratorService : ILinkGeneratorService
{
    public List<LinkDto> GeneratePatientLinks(Guid patientId)
    {
        return new List<LinkDto>
        {
            new("self", $"/api/v1/patients/{patientId}", "GET"),
            new("appointments", $"/api/v1/patients/{patientId}/appointments", "GET"),
            new("invoices", $"/api/v1/patients/{patientId}/invoices", "GET"),
            new("book_appointment", $"/api/v1/appointments", "POST")
        };
    }

    public List<LinkDto> GenerateAppointmentLinks(Guid appointmentId)
    {
        return new List<LinkDto>
        {
            new("self", $"/api/v1/appointments/{appointmentId}", "GET"),
            new("check_in", $"/api/v1/appointments/{appointmentId}/checkin", "PATCH"),
            new("cancel", $"/api/v1/appointments/{appointmentId}/cancel", "PATCH"),
            new("record_vitals", $"/api/v1/vitals", "POST"),
            new("create_consultation", $"/api/v1/consultations", "POST")
        };
    }

    public List<LinkDto> GenerateInvoiceLinks(Guid invoiceId)
    {
        return new List<LinkDto>
        {
            new("self", $"/api/v1/billing/invoices/{invoiceId}", "GET"),
            new("process_payment", $"/api/v1/billing/payments", "POST")
        };
    }
}
