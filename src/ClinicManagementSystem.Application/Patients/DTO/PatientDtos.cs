namespace ClinicManagementSystem.Application.DTOs;

public record CreatePatientDto(
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string Gender,
    string Phone,
    string Email,
    string Address,
    string BloodGroup,
    string EmergencyContact
);

public class PatientResponseDto
{
    public Guid Id { get; set; }
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string BloodGroup { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
}

public class PagedResponse<T>
{
    public PagedResponse() { }
    
    public PagedResponse(IEnumerable<T> items, int totalCount, int page, int pageSize, int totalPages, bool hasNext, bool hasPrevious)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
        TotalPages = totalPages;
        HasNext = hasNext;
        HasPrevious = hasPrevious;
    }

    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNext { get; set; }
    public bool HasPrevious { get; set; }
}
