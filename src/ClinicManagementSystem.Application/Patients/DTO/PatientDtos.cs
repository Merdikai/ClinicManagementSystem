namespace ClinicManagementSystem.Application.DTOs;

public class CreatePatientDto
{
    private string _phone = string.Empty;

    public CreatePatientDto() { }

    public CreatePatientDto(
        string firstName,
        string lastName,
        DateTime dateOfBirth,
        string gender,
        string phone,
        string email,
        string address,
        string bloodGroup,
        string emergencyContact)
    {
        FirstName = firstName;
        LastName = lastName;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        Phone = phone;
        Email = email;
        Address = address;
        BloodGroup = bloodGroup;
        EmergencyContact = emergencyContact;
    }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    
    public string Phone
    {
        get => _phone;
        set => _phone = value ?? string.Empty;
    }

    public string? PhoneNumber
    {
        get => _phone;
        set { if (!string.IsNullOrWhiteSpace(value)) _phone = value; }
    }

    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? BloodGroup { get; set; }
    public string? EmergencyContact { get; set; }
}

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
    public string Address { get; set; } = string.Empty;
    public string BloodGroup { get; set; } = string.Empty;
    public string EmergencyContact { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public List<LinkDto>? Links { get; set; }
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
