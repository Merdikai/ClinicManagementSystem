using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Patients.Queries;

public class MpiLookupResultDto
{
    public Guid Id { get; set; }
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public string? InsuranceProvider { get; set; }
    public string? InsurancePolicyNumber { get; set; }
    public string MatchReason { get; set; } = string.Empty;
    public string ConfidenceLevel { get; set; } = "High";
}

public record MpiLookupQuery(
    string? Query,
    string? NationalId = null,
    string? Phone = null,
    string? FirstName = null,
    string? LastName = null
) : IRequest<List<MpiLookupResultDto>>;

public class MpiLookupQueryHandler : IRequestHandler<MpiLookupQuery, List<MpiLookupResultDto>>
{
    private readonly IClinicDbContext _context;

    public MpiLookupQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<List<MpiLookupResultDto>> Handle(MpiLookupQuery request, CancellationToken cancellationToken)
    {
        var rawQuery = request.Query?.Trim().ToLower() ?? string.Empty;
        var nationalId = request.NationalId?.Trim().ToLower();
        var phone = request.Phone?.Trim().Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        var firstName = request.FirstName?.Trim().ToLower();
        var lastName = request.LastName?.Trim().ToLower();

        if (string.IsNullOrWhiteSpace(rawQuery) && 
            string.IsNullOrWhiteSpace(nationalId) && 
            string.IsNullOrWhiteSpace(phone) && 
            string.IsNullOrWhiteSpace(firstName) && 
            string.IsNullOrWhiteSpace(lastName))
        {
            return new List<MpiLookupResultDto>();
        }

        var baseQuery = _context.Patients.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(nationalId))
        {
            baseQuery = baseQuery.Where(p => p.NationalId != null && p.NationalId.ToLower() == nationalId);
        }
        else if (!string.IsNullOrWhiteSpace(rawQuery))
        {
            baseQuery = baseQuery.Where(p =>
                (p.NationalId != null && p.NationalId.ToLower().Contains(rawQuery)) ||
                (p.MedicalRecordNumber != null && p.MedicalRecordNumber.ToLower().Contains(rawQuery)) ||
                (p.Phone != null && p.Phone.Contains(rawQuery)) ||
                (p.FirstName != null && p.FirstName.ToLower().Contains(rawQuery)) ||
                (p.LastName != null && p.LastName.ToLower().Contains(rawQuery))
            );
        }
        else if (!string.IsNullOrWhiteSpace(phone))
        {
            baseQuery = baseQuery.Where(p => p.Phone != null && p.Phone.Contains(phone));
        }
        else if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
        {
            baseQuery = baseQuery.Where(p => p.FirstName.ToLower() == firstName && p.LastName.ToLower() == lastName);
        }

        var patients = await baseQuery
            .Take(50)
            .ToListAsync(cancellationToken);

        var matches = new List<MpiLookupResultDto>();

        foreach (var p in patients)
        {
            string? matchReason = null;
            string confidence = "Medium";

            // 1. National ID match
            if (!string.IsNullOrWhiteSpace(nationalId) && !string.IsNullOrWhiteSpace(p.NationalId) && p.NationalId.ToLower() == nationalId)
            {
                matchReason = "Exact National ID Match";
                confidence = "Exact";
            }
            else if (!string.IsNullOrWhiteSpace(rawQuery) && !string.IsNullOrWhiteSpace(p.NationalId) && p.NationalId.ToLower().Contains(rawQuery))
            {
                matchReason = "National ID Partial Match";
                confidence = "High";
            }
            // 2. Phone match
            else if (!string.IsNullOrWhiteSpace(phone) && !string.IsNullOrWhiteSpace(p.Phone))
            {
                var cleanDbPhone = p.Phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
                if (cleanDbPhone.Contains(phone) || phone.Contains(cleanDbPhone))
                {
                    matchReason = "Phone Number Match";
                    confidence = "High";
                }
            }
            else if (!string.IsNullOrWhiteSpace(rawQuery) && !string.IsNullOrWhiteSpace(p.Phone) && p.Phone.Contains(rawQuery))
            {
                matchReason = "Phone Match";
                confidence = "High";
            }
            // 3. MRN match
            else if (!string.IsNullOrWhiteSpace(rawQuery) && p.MedicalRecordNumber.ToLower().Contains(rawQuery))
            {
                matchReason = "Medical Record Number (MRN) Match";
                confidence = "Exact";
            }
            // 4. Full name match
            else if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName) &&
                     p.FirstName.ToLower() == firstName && p.LastName.ToLower() == lastName)
            {
                matchReason = "First and Last Name Match";
                confidence = "High";
            }
            // 5. Query matches name
            else if (!string.IsNullOrWhiteSpace(rawQuery) &&
                     ($"{p.FirstName} {p.LastName}".ToLower().Contains(rawQuery) ||
                      $"{p.LastName} {p.FirstName}".ToLower().Contains(rawQuery)))
            {
                matchReason = "Patient Name Match";
                confidence = "Medium";
            }

            if (matchReason != null)
            {
                matches.Add(new MpiLookupResultDto
                {
                    Id = p.Id,
                    MedicalRecordNumber = p.MedicalRecordNumber,
                    FullName = $"{p.FirstName} {p.LastName}",
                    DateOfBirth = p.DateOfBirth,
                    Gender = p.Gender,
                    Phone = p.Phone,
                    NationalId = p.NationalId,
                    InsuranceProvider = p.InsuranceProvider,
                    InsurancePolicyNumber = p.InsurancePolicyNumber,
                    MatchReason = matchReason,
                    ConfidenceLevel = confidence
                });
            }
        }

        return matches.OrderByDescending(m => m.ConfidenceLevel == "Exact")
                      .ThenByDescending(m => m.ConfidenceLevel == "High")
                      .Take(10)
                      .ToList();
    }
}
