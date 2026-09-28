using System;

namespace ClinicManagementSystem.Application.DTOs;

public class CreateVisitorInquiryDto
{
    public string VisitorName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string InquiryType { get; set; } = "General Information";
    public string DepartmentTarget { get; set; } = "Reception";
    public string Notes { get; set; } = string.Empty;
}

public class VisitorInquiryResponseDto
{
    public Guid Id { get; set; }
    public string VisitorName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string InquiryType { get; set; } = string.Empty;
    public string DepartmentTarget { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public Guid HandledByUserId { get; set; }
    public string HandledByUserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
