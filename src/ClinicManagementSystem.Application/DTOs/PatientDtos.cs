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

public record PatientResponseDto(
    Guid Id,
    string MedicalRecordNumber,
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string Gender,
    string Phone,
    string Email,
    string BloodGroup,
    DateTime RegisteredAt
);

public record PagedResponse<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    bool HasNext,
    bool HasPrevious
);