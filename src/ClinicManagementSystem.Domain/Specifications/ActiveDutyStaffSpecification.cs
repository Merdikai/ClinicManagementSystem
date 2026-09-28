using ClinicManagementSystem.Domain.Entities;

namespace ClinicManagementSystem.Domain.Specifications;

public class ActiveDutyStaffSpecification : BaseSpecification<StaffDutyShift>
{
    public ActiveDutyStaffSpecification(string? role = null, string? department = null)
        : base(s => s.IsActive && s.ClockOutTimeUtc == null &&
                    (role == null || s.Role == role) &&
                    (department == null || s.Department == department))
    {
        AddInclude(s => s.User!);
        ApplyOrderBy(s => s.ClockInTimeUtc);
    }
}
