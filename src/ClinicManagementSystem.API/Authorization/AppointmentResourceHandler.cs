using System.Security.Claims;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Prescriptions.DTOs;
using ClinicManagementSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.API.Authorization;

public sealed class AppointmentOperationRequirement : IAuthorizationRequirement
{
    public string OperationName { get; }

    public AppointmentOperationRequirement(string operationName)
    {
        OperationName = operationName;
    }

    public static readonly AppointmentOperationRequirement Read = new("Read");
    public static readonly AppointmentOperationRequirement Modify = new("Modify");
    public static readonly AppointmentOperationRequirement Cancel = new("Cancel");
}

public sealed class ClinicResourceOperationRequirement : IAuthorizationRequirement
{
    public string OperationName { get; }

    public ClinicResourceOperationRequirement(string operationName)
    {
        OperationName = operationName;
    }

    public static readonly ClinicResourceOperationRequirement Read = new("Read");
    public static readonly ClinicResourceOperationRequirement Modify = new("Modify");
}

public sealed class AppointmentResourceHandler : AuthorizationHandler<AppointmentOperationRequirement, Appointment>
{
    private readonly IClinicDbContext _context;

    public AppointmentResourceHandler(IClinicDbContext context)
    {
        _context = context;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AppointmentOperationRequirement requirement,
        Appointment resource)
    {
        if (context.User.IsInRole("Admin") ||
            context.User.IsInRole("Receptionist") ||
            context.User.IsInRole("Nurse"))
        {
            context.Succeed(requirement);
            return;
        }

        var userIdString = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdString, out var userId) && context.User.IsInRole("Doctor"))
        {
            if (requirement == AppointmentOperationRequirement.Read || resource.DoctorId == userId)
            {
                context.Succeed(requirement);
                return;
            }
        }

        var email = context.User.FindFirstValue(ClaimTypes.Email)
            ?? context.User.FindFirstValue("email");
        if (!string.IsNullOrWhiteSpace(email))
        {
            var patient = await _context.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Email == email);

            if (patient != null && resource.PatientId == patient.Id)
            {
                if (requirement == AppointmentOperationRequirement.Read ||
                    requirement == AppointmentOperationRequirement.Cancel)
                {
                    context.Succeed(requirement);
                    return;
                }
            }
        }
    }
}

public sealed class AppointmentDtoResourceHandler : AuthorizationHandler<AppointmentOperationRequirement, AppointmentResponseDto>
{
    private readonly IClinicDbContext _context;

    public AppointmentDtoResourceHandler(IClinicDbContext context)
    {
        _context = context;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AppointmentOperationRequirement requirement,
        AppointmentResponseDto resource)
    {
        if (context.User.IsInRole("Admin") ||
            context.User.IsInRole("Receptionist") ||
            context.User.IsInRole("Nurse"))
        {
            context.Succeed(requirement);
            return;
        }

        var userIdString = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdString, out var userId) && context.User.IsInRole("Doctor"))
        {
            if (requirement == AppointmentOperationRequirement.Read || resource.DoctorId == userId)
            {
                context.Succeed(requirement);
                return;
            }
        }

        var email = context.User.FindFirstValue(ClaimTypes.Email)
            ?? context.User.FindFirstValue("email");
        if (!string.IsNullOrWhiteSpace(email))
        {
            var patient = await _context.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Email == email);

            if (patient != null && resource.PatientId == patient.Id)
            {
                context.Succeed(requirement);
                return;
            }
        }
    }
}

public sealed class PatientResourceHandler : AuthorizationHandler<ClinicResourceOperationRequirement, Guid>
{
    private readonly IClinicDbContext _context;

    public PatientResourceHandler(IClinicDbContext context)
    {
        _context = context;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ClinicResourceOperationRequirement requirement,
        Guid patientId)
    {
        if (requirement == ClinicResourceOperationRequirement.Read &&
            (context.User.IsInRole("Admin") || context.User.IsInRole("Doctor") ||
             context.User.IsInRole("Nurse") || context.User.IsInRole("Receptionist") ||
             context.User.IsInRole("Pharmacist") || context.User.IsInRole("LabTechnician") ||
             context.User.IsInRole("Radiologist") || context.User.IsInRole("Accountant")))
        {
            context.Succeed(requirement);
            return;
        }

        if (requirement == ClinicResourceOperationRequirement.Modify &&
            (context.User.IsInRole("Admin") || context.User.IsInRole("Doctor") ||
             context.User.IsInRole("Nurse") || context.User.IsInRole("Receptionist")))
        {
            context.Succeed(requirement);
            return;
        }

        var email = context.User.FindFirstValue(ClaimTypes.Email)
            ?? context.User.FindFirstValue("email");
        if (!string.IsNullOrWhiteSpace(email))
        {
            var patient = await _context.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Email == email);

            if (patient != null && patient.Id == patientId &&
                requirement == ClinicResourceOperationRequirement.Read)
            {
                context.Succeed(requirement);
                return;
            }
        }
    }
}

public sealed class InvoiceResourceHandler : AuthorizationHandler<ClinicResourceOperationRequirement, InvoiceResponseDto>
{
    private readonly IClinicDbContext _context;

    public InvoiceResourceHandler(IClinicDbContext context)
    {
        _context = context;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ClinicResourceOperationRequirement requirement,
        InvoiceResponseDto invoice)
    {
        if (context.User.IsInRole("Admin") ||
            context.User.IsInRole("Accountant") ||
            context.User.IsInRole("Receptionist") ||
            context.User.IsInRole("Doctor"))
        {
            context.Succeed(requirement);
            return;
        }

        if (context.User.IsInRole("Pharmacist"))
        {
            var isPharmacy = string.Equals(invoice.Department, "Pharmacy", StringComparison.OrdinalIgnoreCase)
                || (invoice.InvoiceNumber != null && invoice.InvoiceNumber.StartsWith("INV-RX", StringComparison.OrdinalIgnoreCase));
            if (isPharmacy)
            {
                context.Succeed(requirement);
                return;
            }
        }

        if (context.User.IsInRole("LabTechnician"))
        {
            var isLab = string.Equals(invoice.Department, "Laboratory", StringComparison.OrdinalIgnoreCase)
                || (invoice.InvoiceNumber != null && invoice.InvoiceNumber.StartsWith("INV-LAB", StringComparison.OrdinalIgnoreCase));
            if (isLab)
            {
                context.Succeed(requirement);
                return;
            }
        }

        if (context.User.IsInRole("Patient"))
        {
            var email = context.User.FindFirstValue(ClaimTypes.Email)
                ?? context.User.FindFirstValue("email");
            if (!string.IsNullOrWhiteSpace(email))
            {
                var patient = await _context.Patients
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Email == email);

                if (patient != null && invoice.PatientId == patient.Id)
                {
                    context.Succeed(requirement);
                    return;
                }
            }
        }
    }
}

public sealed class PrescriptionResourceHandler : AuthorizationHandler<ClinicResourceOperationRequirement, PrescriptionDto>
{
    private readonly IClinicDbContext _context;

    public PrescriptionResourceHandler(IClinicDbContext context)
    {
        _context = context;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ClinicResourceOperationRequirement requirement,
        PrescriptionDto prescription)
    {
        if (context.User.IsInRole("Admin") ||
            context.User.IsInRole("Pharmacist") ||
            context.User.IsInRole("Doctor") ||
            context.User.IsInRole("Nurse"))
        {
            context.Succeed(requirement);
            return;
        }

        if (context.User.IsInRole("Patient"))
        {
            var email = context.User.FindFirstValue(ClaimTypes.Email)
                ?? context.User.FindFirstValue("email");
            if (!string.IsNullOrWhiteSpace(email))
            {
                var patient = await _context.Patients
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Email == email);

                if (patient != null && prescription.PatientId == patient.Id)
                {
                    context.Succeed(requirement);
                    return;
                }
            }
        }
    }
}
