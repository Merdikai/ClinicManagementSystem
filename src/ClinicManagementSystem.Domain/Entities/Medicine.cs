namespace ClinicManagementSystem.Domain.Entities;

public class Medicine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public decimal UnitPrice { get; set; }
}