using System;

namespace ClinicManagementSystem.Domain.Entities;

public class PharmacyStockRequisition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RequisitionNumber { get; set; } = string.Empty;
    public string Type { get; set; } = "PurchaseOrder"; // "PurchaseOrder", "WardRequisition", "EmergencyCartRefill"
    public string Department { get; set; } = "Main Pharmacy";
    
    public Guid MedicineId { get; set; }
    public Medicine Medicine { get; set; } = null!;
    
    public int QuantityRequested { get; set; }
    public int QuantityApproved { get; set; }
    public decimal EstimatedUnitCost { get; set; }
    public decimal TotalAmount { get; set; }
    public string? SupplierName { get; set; }
    public string Status { get; set; } = "Pending"; // "Pending", "Approved", "Received", "Rejected"
    
    public Guid RequestedByUserId { get; set; }
    public User RequestedByUser { get; set; } = null!;
    
    public Guid? ApprovedByUserId { get; set; }
    public User? ApprovedByUser { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}
