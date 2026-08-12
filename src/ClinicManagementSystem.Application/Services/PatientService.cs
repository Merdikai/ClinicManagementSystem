using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;

namespace ClinicManagementSystem.Application.Services;

public class PatientService : IPatientService
{
    private readonly IPatientRepository _patientRepository;
    private readonly IMapper _mapper;

    public PatientService(IPatientRepository patientRepository, IMapper mapper)
    {
        _patientRepository = patientRepository;
        _mapper = mapper;
    }

    public async Task<PatientResponseDto> CreateAsync(CreatePatientDto dto)
    {
        var patient = _mapper.Map<Patient>(dto);
        patient.MedicalRecordNumber = GenerateMRN();

        await _patientRepository.AddAsync(patient);
        await _patientRepository.SaveChangesAsync();
        return _mapper.Map<PatientResponseDto>(patient);
    }

    public async Task<PatientResponseDto?> GetByIdAsync(Guid id)
    {
        var patient = await _patientRepository.GetByIdAsync(id);
        if (patient is null) throw new NotFoundException(nameof(Patient), id);
        return _mapper.Map<PatientResponseDto>(patient);
    }

    public async Task<PagedResponse<PatientResponseDto>> GetPagedAsync(int page, int pageSize, string? search)
    {
        var (items, totalCount) = await _patientRepository.GetPagedAsync(page, pageSize, search);
        var dtos = _mapper.Map<IEnumerable<PatientResponseDto>>(items);

        return new PagedResponse<PatientResponseDto>(
            dtos,
            totalCount,
            page,
            pageSize,
            (int)Math.Ceiling(totalCount / (double)pageSize),
            page < (int)Math.Ceiling(totalCount / (double)pageSize),
            page > 1
        );
    }

    private string GenerateMRN()
    {
        var year = DateTime.UtcNow.Year;
        var random = new Random().Next(10000, 99999);
        return $"PAT-{year}-{random}";
    }
}