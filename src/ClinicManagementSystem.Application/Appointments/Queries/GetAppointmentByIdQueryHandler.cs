using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Appointments.Queries;

using ClinicManagementSystem.Application.Interfaces;

public class GetAppointmentByIdQueryHandler : IRequestHandler<GetAppointmentByIdQuery, AppointmentResponseDto>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IMapper _mapper;
    private readonly ILinkGeneratorService _linkGenerator;

    public GetAppointmentByIdQueryHandler(IAppointmentRepository appointmentRepository, IMapper mapper, ILinkGeneratorService linkGenerator)
    {
        _appointmentRepository = appointmentRepository;
        _mapper = mapper;
        _linkGenerator = linkGenerator;
    }

    public async Task<AppointmentResponseDto> Handle(GetAppointmentByIdQuery request, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException(nameof(Appointment), request.Id);
        var dto = _mapper.Map<AppointmentResponseDto>(appointment);
        dto.Links = _linkGenerator.GenerateAppointmentLinks(appointment.Id);
        return dto;
    }
}
