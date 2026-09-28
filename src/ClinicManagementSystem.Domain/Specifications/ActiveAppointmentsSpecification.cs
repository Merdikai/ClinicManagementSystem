using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;

namespace ClinicManagementSystem.Domain.Specifications;

public class ActiveAppointmentsSpecification : BaseSpecification<Appointment>
{
    public ActiveAppointmentsSpecification(Guid? doctorId = null)
        : base(a => (doctorId == null || a.DoctorId == doctorId) &&
                    (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.CheckedIn))
    {
        AddInclude(a => a.Patient);
        AddInclude(a => a.Doctor);
        ApplyOrderBy(a => a.ScheduledDateTime);
    }
}
