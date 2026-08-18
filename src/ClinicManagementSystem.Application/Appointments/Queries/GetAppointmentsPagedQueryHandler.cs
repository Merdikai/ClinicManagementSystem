using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Appointments.Queries;

public class GetAppointmentsPagedQueryHandler : IRequestHandler<GetAppointmentsPagedQuery, PagedResponse<AppointmentResponseDto>>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IMapper _mapper;
    private readonly ILinkGeneratorService _linkGenerator;

    public GetAppointmentsPagedQueryHandler(
        IAppointmentRepository appointmentRepository,
        IMapper mapper,
        ILinkGeneratorService linkGenerator)
    {
        _appointmentRepository = appointmentRepository;
        _mapper = mapper;
        _linkGenerator = linkGenerator;
    }

    public async Task<PagedResponse<AppointmentResponseDto>> Handle(GetAppointmentsPagedQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _appointmentRepository.GetPagedAsync(request.Page, request.PageSize, request.StartDate, request.EndDate);
        var dtos = _mapper.Map<IEnumerable<AppointmentResponseDto>>(items).ToList();

        foreach (var dto in dtos)
        {
            var entity = items.FirstOrDefault(a => a.Id == dto.Id);
            if (entity is not null)
            {
                dto.Links = _linkGenerator.GenerateAppointmentLinks(entity);
            }
        }

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new PagedResponse<AppointmentResponseDto>(
            dtos, totalCount, request.Page, request.PageSize,
            totalPages,
            request.Page < totalPages,
            request.Page > 1
        );
    }
}
