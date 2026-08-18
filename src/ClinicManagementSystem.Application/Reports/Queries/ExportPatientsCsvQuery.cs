using System.Text;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Reports.Queries;

public record ExportPatientsCsvQuery : IRequest<byte[]>;

public class ExportPatientsCsvQueryHandler : IRequestHandler<ExportPatientsCsvQuery, byte[]>
{
    private readonly IClinicDbContext _context;

    public ExportPatientsCsvQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<byte[]> Handle(ExportPatientsCsvQuery request, CancellationToken cancellationToken)
    {
        var patients = await _context.Patients.ToListAsync(cancellationToken);
        var sb = new StringBuilder();
        
        sb.AppendLine("MRN,FirstName,LastName,DateOfBirth,Gender,Phone,Email,BloodGroup");
        
        foreach (var p in patients)
        {
            sb.AppendLine($"{p.MedicalRecordNumber},{p.FirstName},{p.LastName},{p.DateOfBirth:yyyy-MM-dd},{p.Gender},{p.Phone},{p.Email},{p.BloodGroup}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
