using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ClinicManagementSystem.Application.Reports.Queries;

public record ExportPatientsCsvQuery : IRequest<byte[]>;

public class ExportPatientsCsvQueryHandler : IRequestHandler<ExportPatientsCsvQuery, byte[]>
{
    private readonly IClinicDbContext _context;

    public ExportPatientsCsvQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }

    public async Task<byte[]> Handle(ExportPatientsCsvQuery request, CancellationToken cancellationToken)
    {
        var patients = await _context.Patients
            .OrderBy(p => p.LastName)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("MRN,FirstName,LastName,DateOfBirth,Gender,Phone,Email,BloodGroup");

        foreach (var p in patients)
        {
            sb.AppendLine($"{EscapeCsv(p.MedicalRecordNumber)},{EscapeCsv(p.FirstName)},{EscapeCsv(p.LastName)},{p.DateOfBirth:yyyy-MM-dd},{EscapeCsv(p.Gender)},{EscapeCsv(p.Phone)},{EscapeCsv(p.Email)},{EscapeCsv(p.BloodGroup)}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
