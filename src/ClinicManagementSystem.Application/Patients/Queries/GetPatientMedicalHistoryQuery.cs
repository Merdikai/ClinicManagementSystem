using AutoMapper;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Patients.Queries;

public record GetPatientMedicalHistoryQuery(Guid PatientId) : IRequest<Result<MedicalHistoryResponseDto>>;

public class GetPatientMedicalHistoryQueryHandler : IRequestHandler<GetPatientMedicalHistoryQuery, Result<MedicalHistoryResponseDto>>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IMapper _mapper;

    public GetPatientMedicalHistoryQueryHandler(
        IPatientRepository patientRepository,
        IAppointmentRepository appointmentRepository,
        IMapper mapper)
    {
        _patientRepository = patientRepository;
        _appointmentRepository = appointmentRepository;
        _mapper = mapper;
    }

    public async Task<Result<MedicalHistoryResponseDto>> Handle(GetPatientMedicalHistoryQuery request, CancellationToken cancellationToken)
    {
        var patient = await _patientRepository.GetByIdAsync(request.PatientId);
        if (patient is null)
            return Result<MedicalHistoryResponseDto>.Failure("Patient not found", "patient_not_found");

        var appointments = await _appointmentRepository.GetByPatientIdAsync(request.PatientId);
        var visits = new List<VisitRecordDto>();

        foreach (var appointment in appointments.Where(a => a.Status == Domain.Enums.AppointmentStatus.Completed))
        {
            var consultation = appointment.Consultation;
            visits.Add(new VisitRecordDto(
                appointment.Id,
                appointment.ScheduledDateTime,
                $"{appointment.Doctor?.FirstName} {appointment.Doctor?.LastName}",
                appointment.ReasonForVisit,
                consultation?.Diagnosis ?? "No diagnosis",
                consultation?.Symptoms ?? "No symptoms recorded",
                appointment.VitalSign is not null ? _mapper.Map<VitalSignResponseDto>(appointment.VitalSign) : null,
                consultation?.Prescription is not null ? _mapper.Map<PrescriptionResponseDto>(consultation.Prescription) : null
            ));
        }

        var history = new MedicalHistoryResponseDto(
            request.PatientId,
            $"{patient.FirstName} {patient.LastName}",
            visits.OrderByDescending(v => v.VisitDate)
        );

        return Result<MedicalHistoryResponseDto>.Success(history);
    }
}
