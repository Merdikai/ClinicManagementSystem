using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ClinicManagementSystem.Application.Reports.Queries;

public record ExportPatientsCsvQuery(DateTime? StartDate = null, DateTime? EndDate = null, string? Timeframe = null) : IRequest<byte[]>;

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
        var query = _context.Patients.AsNoTracking().AsQueryable();

        if (request.StartDate.HasValue)
        {
            var startUtc = DateTime.SpecifyKind(request.StartDate.Value, DateTimeKind.Utc);
            query = query.Where(p => p.RegisteredAt >= startUtc);
        }

        if (request.EndDate.HasValue)
        {
            var endUtc = DateTime.SpecifyKind(request.EndDate.Value, DateTimeKind.Utc);
            query = query.Where(p => p.RegisteredAt <= endUtc);
        }

        var patients = await query
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("MRN,FirstName,LastName,DateOfBirth,Gender,Phone,Email,Address,BloodGroup,InsuranceProvider,PolicyNumber,RegistrationDate");

        foreach (var p in patients)
        {
            sb.AppendLine($"{EscapeCsv(p.MedicalRecordNumber)},{EscapeCsv(p.FirstName)},{EscapeCsv(p.LastName)},{p.DateOfBirth:yyyy-MM-dd},{EscapeCsv(p.Gender)},{EscapeCsv(p.Phone)},{EscapeCsv(p.Email)},{EscapeCsv(p.Address)},{EscapeCsv(p.BloodGroup)},{EscapeCsv(p.InsuranceProvider)},{EscapeCsv(p.InsurancePolicyNumber)},{p.RegisteredAt:yyyy-MM-dd HH:mm}");
        }

        // Prepend UTF-8 BOM for Microsoft Excel compatibility
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var textBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[bom.Length + textBytes.Length];
        Buffer.BlockCopy(bom, 0, result, 0, bom.Length);
        Buffer.BlockCopy(textBytes, 0, result, bom.Length, textBytes.Length);

        return result;
    }
}
