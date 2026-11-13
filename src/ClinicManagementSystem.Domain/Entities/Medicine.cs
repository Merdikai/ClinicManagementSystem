namespace ClinicManagementSystem.Domain.Entities;

public class Medicine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code
    {
        get;
        set => field = value?.Trim() ?? string.Empty;
    } = string.Empty;
    public string Name
    {
        get;
        set => field = value?.Trim() ?? string.Empty;
    } = string.Empty;
    public string GenericName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string DosageForm { get; set; } = "Tablet";
    public string Strength { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public int StockQuantity
    {
        get;
        set => field = value < 0
            ? throw new ArgumentOutOfRangeException(nameof(StockQuantity), "Stock quantity cannot be negative.")
            : value;
    }
    public int ReorderLevel
    {
        get;
        set => field = value < 0 ? 0 : value;
    } = 15;
    public decimal CostPrice
    {
        get;
        set => field = value < 0m
            ? throw new ArgumentOutOfRangeException(nameof(CostPrice), "Cost price cannot be negative.")
            : value;
    }
    public decimal UnitPrice
    {
        get;
        set => field = value < 0m
            ? throw new ArgumentOutOfRangeException(nameof(UnitPrice), "Unit price cannot be negative.")
            : value;
    }
    public DateTime? ExpiryDate { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public string StorageConditions { get; set; } = "Room temperature (15-25°C)";
    public bool IsControlledSubstance { get; set; } = false;
    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public uint RowVersion { get; set; }
}
