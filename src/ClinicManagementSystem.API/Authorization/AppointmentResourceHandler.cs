using System.Security.Claims;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.API.Authorization;

public class AppointmentOperationRequirement : IAuthorizationRequirement
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

public class AppointmentResourceHandler : AuthorizationHandler<AppointmentOperationRequirement, Appointment>
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
        // Admins have universal management authority
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return;
        }

        var userIdString = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdString, out var userId))
        {
            // If user is the attending doctor for this appointment
            if (context.User.IsInRole("Doctor") && resource.DoctorId == userId)
            {
                context.Succeed(requirement);
                return;
            }
        }

        // If user is the patient owning this appointment
        var email = context.User.FindFirstValue(ClaimTypes.Email);
        if (!string.IsNullOrEmpty(email))
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

        // Per Qiyas M11 S3: Gracefully complete task without succeeding if unauthorized (no null-ref/exceptions)
    }
}
