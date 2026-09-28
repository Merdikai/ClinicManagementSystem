namespace ClinicManagementSystem.Domain.Entities;

public class RolePermission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public string Permission { get; set; } = string.Empty;
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
}
