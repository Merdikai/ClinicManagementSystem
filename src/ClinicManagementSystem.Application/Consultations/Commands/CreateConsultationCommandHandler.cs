using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Consultations.Commands;

public class CreateConsultationCommandHandler : IRequestHandler<CreateConsultationCommand, ConsultationResponseDto>
{
    private readonly IConsultationRepository _consultationRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IMapper _mapper;

    public CreateConsultationCommandHandler(
        IConsultationRepository consultationRepository,
        IAppointmentRepository appointmentRepository,
        IMapper mapper)
    {
        _consultationRepository = consultationRepository;
        _appointmentRepository = appointmentRepository;
        _mapper = mapper;
    }

    public async Task<ConsultationResponseDto> Handle(CreateConsultationCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId)
            ?? throw new NotFoundException(nameof(Appointment), request.AppointmentId);

        var dto = new CreateConsultationDto(request.AppointmentId, request.Symptoms, request.Diagnosis, request.ClinicalNotes);
        var consultation = _mapper.Map<Consultation>(dto);
        consultation.DoctorId = request.DoctorId;

        await _consultationRepository.AddAsync(consultation);
        await _consultationRepository.SaveChangesAsync();
        return _mapper.Map<ConsultationResponseDto>(consultation);
    }
}
