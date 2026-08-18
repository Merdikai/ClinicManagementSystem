using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Patients.Queries;

public record GetPatientAppointmentsQuery(Guid PatientId) : IRequest<IEnumerable<AppointmentResponseDto>>;

public class GetPatientAppointmentsQueryHandler : IRequestHandler<GetPatientAppointmentsQuery, IEnumerable<AppointmentResponseDto>>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IMapper _mapper;
    private readonly ILinkGeneratorService _linkGenerator;

    public GetPatientAppointmentsQueryHandler(
        IAppointmentRepository appointmentRepository,
        IMapper mapper,
        ILinkGeneratorService linkGenerator)
    {
        _appointmentRepository = appointmentRepository;
        _mapper = mapper;
        _linkGenerator = linkGenerator;
    }

    public async Task<IEnumerable<AppointmentResponseDto>> Handle(GetPatientAppointmentsQuery request, CancellationToken cancellationToken)
    {
        var appointments = await _appointmentRepository.GetByPatientIdAsync(request.PatientId);
        var dtos = _mapper.Map<IEnumerable<AppointmentResponseDto>>(appointments).ToList();

        foreach (var dto in dtos)
        {
            var entity = appointments.FirstOrDefault(a => a.Id == dto.Id);
            if (entity is not null)
            {
                dto.Links = _linkGenerator.GenerateAppointmentLinks(entity);
            }
        }

        return dtos;
    }
}
