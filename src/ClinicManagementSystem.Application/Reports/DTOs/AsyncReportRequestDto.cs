namespace ClinicManagementSystem.Application.Reports.DTOs;

public class AsyncReportRequestDto
{
    public string? ReportType { get; set; } = "ClinicalExecutiveSummary";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Format { get; set; } = "PDF";
}

