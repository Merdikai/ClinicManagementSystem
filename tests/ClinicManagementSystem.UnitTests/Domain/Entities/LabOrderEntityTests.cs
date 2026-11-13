using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace ClinicManagementSystem.UnitTests.Domain.Entities;

public class LabOrderEntityTests
{
    [Fact]
    public void CreateLabOrder_DefaultState_ShouldBeOrderedAndNotBilled()
    {
        var order = new LabOrder
        {
            PatientId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            OrderNumber = "LAB-2026-0001",
            Priority = LabOrderPriority.Routine
        };

        order.Status.Should().Be(LabOrderStatus.Ordered);
        order.IsBilled.Should().BeFalse();
        order.IsRejected.Should().BeFalse();
        order.RequiresRecollection.Should().BeFalse();
        order.Items.Should().BeEmpty();
    }

    [Fact]
    public void RejectSpecimen_ShouldSetRejectedFlagAndReason()
    {
        var order = new LabOrder
        {
            PatientId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            SpecimenBarcode = "BC-123456"
        };

        order.IsRejected = true;
        order.RejectionReason = "Hemolyzed specimen";
        order.RejectedAt = DateTime.UtcNow;
        order.RejectedByTechnicianName = "Lab Tech Alex";
        order.RequiresRecollection = true;
        order.Status = LabOrderStatus.SampleCollected;

        order.IsRejected.Should().BeTrue();
        order.RejectionReason.Should().Be("Hemolyzed specimen");
        order.RequiresRecollection.Should().BeTrue();
        order.RejectedByTechnicianName.Should().Be("Lab Tech Alex");
    }

    [Fact]
    public void AddItems_TotalCost_ShouldSumCorrectly()
    {
        var order = new LabOrder();
        order.Items.Add(new LabOrderItem { LabTestTemplateId = Guid.NewGuid(), Price = 45.00m });
        order.Items.Add(new LabOrderItem { LabTestTemplateId = Guid.NewGuid(), Price = 55.00m });

        order.TotalCost = order.Items.Sum(i => i.Price);

        order.TotalCost.Should().Be(100.00m);
        order.Items.Count.Should().Be(2);
    }
}

