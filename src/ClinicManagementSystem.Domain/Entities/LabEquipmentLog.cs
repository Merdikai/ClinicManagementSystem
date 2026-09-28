using System;

namespace ClinicManagementSystem.Domain.Entities;

public class LabEquipmentLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EquipmentName { get; set; } = string.Empty;
    public string EquipmentModel { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Location { get; set; } = "Main Laboratory";
    public string LogType { get; set; } = "DailyCalibration"; // DailyCalibration, QualityControl, PreventiveMaintenance, BreakdownRepair
    public string? ControlLotNumber { get; set; }
    public decimal? StandardDeviationScore { get; set; }
    public string Status { get; set; } = "Passed"; // Passed, Failed, Recalibrated, Scheduled, Completed
    public string Findings { get; set; } = string.Empty;
    public string? CorrectiveAction { get; set; }
    
    public Guid PerformedByUserId { get; set; }
    public string PerformedByUserName { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    public DateTime? NextMaintenanceDueDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
