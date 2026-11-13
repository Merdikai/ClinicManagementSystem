using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using ClinicManagementSystem.Domain.Specifications;
using FluentAssertions;
using Xunit;

namespace ClinicManagementSystem.UnitTests.Domain.Specifications;

public class SpecificationsTests
{
    [Fact]
    public void LowStockMedicinesSpecification_Criteria_ShouldFilterActiveAndBelowReorder()
    {
        var spec = new LowStockMedicinesSpecification();
        var filter = spec.Criteria!.Compile();

        var lowStock = new Medicine { IsActive = true, StockQuantity = 5, ReorderLevel = 10 };
        var ampleStock = new Medicine { IsActive = true, StockQuantity = 50, ReorderLevel = 10 };
        var inactive = new Medicine { IsActive = false, StockQuantity = 2, ReorderLevel = 10 };

        filter(lowStock).Should().BeTrue();
        filter(ampleStock).Should().BeFalse();
        filter(inactive).Should().BeFalse();
    }

    [Fact]
    public void ActiveAppointmentsSpecification_Criteria_ShouldFilterScheduledAndCheckedIn()
    {
        var doctorId = Guid.NewGuid();
        var spec = new ActiveAppointmentsSpecification(doctorId);
        var filter = spec.Criteria!.Compile();

        var matchingScheduled = new Appointment { DoctorId = doctorId, Status = AppointmentStatus.Scheduled };
        var matchingCheckedIn = new Appointment { DoctorId = doctorId, Status = AppointmentStatus.CheckedIn };
        var completed = new Appointment { DoctorId = doctorId, Status = AppointmentStatus.Completed };
        var otherDoctor = new Appointment { DoctorId = Guid.NewGuid(), Status = AppointmentStatus.Scheduled };

        filter(matchingScheduled).Should().BeTrue();
        filter(matchingCheckedIn).Should().BeTrue();
        filter(completed).Should().BeFalse();
        filter(otherDoctor).Should().BeFalse();
    }

    [Fact]
    public void PendingLabOrdersSpecification_Criteria_ShouldExcludeCompletedAndCancelled()
    {
        var spec = new PendingLabOrdersSpecification();
        var filter = spec.Criteria!.Compile();

        var ordered = new LabOrder { Status = LabOrderStatus.Ordered };
        var inProgress = new LabOrder { Status = LabOrderStatus.InProgress };
        var completed = new LabOrder { Status = LabOrderStatus.Completed };
        var cancelled = new LabOrder { Status = LabOrderStatus.Cancelled };

        filter(ordered).Should().BeTrue();
        filter(inProgress).Should().BeTrue();
        filter(completed).Should().BeFalse();
        filter(cancelled).Should().BeFalse();
    }

    [Fact]
    public void PendingRadiologyOrdersSpecification_Criteria_ShouldExcludeCompletedAndCancelled()
    {
        var spec = new PendingRadiologyOrdersSpecification();
        var filter = spec.Criteria!.Compile();

        var ordered = new RadiologyOrder { Status = "Ordered" };
        var inProgress = new RadiologyOrder { Status = "InProgress" };
        var completed = new RadiologyOrder { Status = "Completed" };
        var cancelled = new RadiologyOrder { Status = "Cancelled" };

        filter(ordered).Should().BeTrue();
        filter(inProgress).Should().BeTrue();
        filter(completed).Should().BeFalse();
        filter(cancelled).Should().BeFalse();
    }
}

