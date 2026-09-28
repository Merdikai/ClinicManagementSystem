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

    public List<LinkDto> GenerateAppointmentLinks(ClinicManagementSystem.Domain.Entities.Appointment appointment)
    {
        var links = new List<LinkDto>
        {
            new("self", $"/api/v1/appointments/{appointment.Id}", "GET"),
            new("cancel", $"/api/v1/appointments/{appointment.Id}/cancel", "PATCH")
        };

        if (appointment.Status == ClinicManagementSystem.Domain.Enums.AppointmentStatus.Scheduled)
        {
            links.Add(new("check_in", $"/api/v1/appointments/{appointment.Id}/checkin", "PATCH"));
        }

        if (appointment.Status == ClinicManagementSystem.Domain.Enums.AppointmentStatus.CheckedIn)
        {
            links.Add(new("record_vitals", $"/api/v1/vitals", "POST"));
            links.Add(new("create_consultation", $"/api/v1/consultations", "POST"));
        }

        return links;
    }

    public List<LinkDto> GenerateInvoiceLinks(Guid invoiceId)
    {
        return new List<LinkDto>
        {
            new("self", $"/api/v1/billing/invoices/{invoiceId}", "GET"),
            new("process_payment", $"/api/v1/billing/payments", "POST")
        };
    }

    public List<LinkDto> GenerateMedicineLinks(Guid medicineId)
    {
        return new List<LinkDto>
        {
            new("self", $"/api/v1/medicines/{medicineId}", "GET"),
            new("dispense", $"/api/v1/medicines/{medicineId}/dispense", "PATCH")
        };
    }

    public List<LinkDto> GenerateRadiologyOrderLinks(Guid orderId, string status)
    {
        var links = new List<LinkDto>
        {
            new("self", $"/api/v1/radiology/orders/{orderId}", "GET")
        };

        if (status == "Ordered")
        {
            links.Add(new("schedule", $"/api/v1/radiology/orders/{orderId}/schedule", "POST"));
            links.Add(new("start_scan", $"/api/v1/radiology/orders/{orderId}/status", "PUT"));
        }
        else if (status == "InProgress")
        {
            links.Add(new("report", $"/api/v1/radiology/orders/{orderId}/report", "POST"));
        }

        return links;
    }

    public List<LinkDto> GenerateLabOrderLinks(Guid orderId, string status)
    {
        var links = new List<LinkDto>
        {
            new("self", $"/api/v1/laborders/{orderId}", "GET")
        };

        if (status == "Ordered")
        {
            links.Add(new("intake_specimen", $"/api/v1/laborders/{orderId}/specimen-intake", "POST"));
        }
        else if (status == "SampleCollected" || status == "InProgress")
        {
            links.Add(new("enter_results", $"/api/v1/laborders/{orderId}/results", "POST"));
        }

        return links;
    }
}
