using ClinicManagementSystem.Application.DTOs;
using MediatR;

namespace ClinicManagementSystem.Application.Consultations.Commands;

public record CreateConsultationCommand(
    Guid AppointmentId,
    string Symptoms,
    string Diagnosis,
    string ClinicalNotes,
    Guid DoctorId
) : IRequest<ConsultationResponseDto>;
