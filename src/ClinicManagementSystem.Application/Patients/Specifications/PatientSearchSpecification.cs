using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Specifications;

namespace ClinicManagementSystem.Application.Patients.Specifications;

public class PatientSearchSpecification : BaseSpecification<Patient>
{
    public PatientSearchSpecification(string? search, int page = 1, int pageSize = 10)
        : base(p => string.IsNullOrWhiteSpace(search) ||
                    p.FirstName.ToLower().Contains(search.ToLower()) ||
                    p.LastName.ToLower().Contains(search.ToLower()) ||
                    p.MedicalRecordNumber.ToLower().Contains(search.ToLower()) ||
                    p.Phone.Contains(search) ||
                    p.Email.ToLower().Contains(search.ToLower()))
    {
        ApplyPaging((page - 1) * pageSize, pageSize);
        ApplyOrderByDescending(p => p.RegisteredAt);
    }
}
