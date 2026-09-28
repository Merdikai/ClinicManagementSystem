using System;

namespace ClinicManagementSystem.Domain.Entities;

public class VisitorInquiry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string VisitorName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string InquiryType { get; set; } = "General Information"; // "General Information", "Doctor Availability", "Appointment Scheduling", "Pricing & Insurance", "Directions"
    public string DepartmentTarget { get; set; } = "Reception"; // "Reception", "Nursing", "Pharmacy", "Billing", "Physician"
    public string Notes { get; set; } = string.Empty;
    public string Status { get; set; } = "Open"; // "Open", "Resolved", "Forwarded"
    public Guid HandledByUserId { get; set; }
    public User HandledByUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}
