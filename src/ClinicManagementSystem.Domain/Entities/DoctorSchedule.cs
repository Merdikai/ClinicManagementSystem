namespace ClinicManagementSystem.Domain.Entities;

public class DoctorSchedule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DoctorId { get; set; }
    public User Doctor { get; set; } = null!;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int SlotDurationMinutes { get; set; } = 30;
    public bool IsActive { get; set; } = true;
}