namespace ClinicManagementSystem.Domain.Entities;

public class Patient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string BloodGroup { get; set; } = string.Empty;
    public string EmergencyContact { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}