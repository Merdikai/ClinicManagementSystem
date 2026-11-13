using ClinicManagementSystem.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ClinicManagementSystem.UnitTests.Domain.Entities;

public class MedicineEntityTests
{
    [Fact]
    public void Medicine_SettingValidValues_ShouldSucceedAndTrim()
    {
        var med = new Medicine
        {
            Code = "  MED-001  ",
            Name = "  Amoxicillin  ",
            StockQuantity = 100,
            ReorderLevel = 20,
            CostPrice = 5.50m,
            UnitPrice = 12.00m
        };

        med.Code.Should().Be("MED-001");
        med.Name.Should().Be("Amoxicillin");
        med.StockQuantity.Should().Be(100);
        med.ReorderLevel.Should().Be(20);
        med.CostPrice.Should().Be(5.50m);
        med.UnitPrice.Should().Be(12.00m);
    }

    [Fact]
    public void Medicine_NegativeStockQuantity_ShouldThrowArgumentOutOfRangeException()
    {
        var med = new Medicine();
        var act = () => med.StockQuantity = -5;

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("StockQuantity");
    }

    [Fact]
    public void Medicine_NegativeCostPrice_ShouldThrowArgumentOutOfRangeException()
    {
        var med = new Medicine();
        var act = () => med.CostPrice = -1.00m;

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("CostPrice");
    }

    [Fact]
    public void Medicine_NegativeUnitPrice_ShouldThrowArgumentOutOfRangeException()
    {
        var med = new Medicine();
        var act = () => med.UnitPrice = -0.50m;

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("UnitPrice");
    }

    [Fact]
    public void Medicine_NegativeReorderLevel_ShouldClampToZero()
    {
        var med = new Medicine
        {
            ReorderLevel = -10
        };

        med.ReorderLevel.Should().Be(0);
    }
}

