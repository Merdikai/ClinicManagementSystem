using System;

namespace ClinicManagementSystem.Domain.Entities;

public class LabStockRequisition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RequisitionNumber { get; set; } = string.Empty;
    
    public Guid? LabReagentId { get; set; }
    public LabReagent? LabReagent { get; set; }

    public string ItemName { get; set; } = string.Empty;
    public string Category { get; set; } = "Hematology";
    public int QuantityRequested { get; set; }
    public string UnitOfMeasure { get; set; } = "Kits";
    public decimal EstimatedUnitCost { get; set; }
    public decimal TotalEstimatedCost { get; set; }
    public string Urgency { get; set; } = "Routine"; // Routine, Urgent, Stat
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Approved, Fulfilled, Rejected

    public Guid RequestedByUserId { get; set; }
    public User? RequestedByUser { get; set; }
    public string RequestedByUserName { get; set; } = string.Empty;

    public Guid? ApprovedByUserId { get; set; }
    public User? ApprovedByUser { get; set; }
    public string? ApprovedByUserName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
