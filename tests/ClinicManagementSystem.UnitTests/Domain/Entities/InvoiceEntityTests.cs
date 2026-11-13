using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace ClinicManagementSystem.UnitTests.Domain.Entities;

public class InvoiceEntityTests
{
    [Fact]
    public void Invoice_BalanceDue_ShouldReflectTotalMinusPayments()
    {
        var invoice = new Invoice
        {
            TotalAmount = 250.00m,
            SubTotal = 220.00m,
            TaxAmount = 30.00m,
            DiscountAmount = 0m,
            Payments = new List<Payment>
            {
                new() { AmountPaid = 100.00m },
                new() { AmountPaid = 50.00m }
            }
        };

        invoice.BalanceDue.Should().Be(100.00m);
    }

    [Fact]
    public void Invoice_ZeroPayments_BalanceDueShouldEqualTotalAmount()
    {
        var invoice = new Invoice
        {
            TotalAmount = 175.50m
        };

        invoice.BalanceDue.Should().Be(175.50m);
    }

    [Theory]
    [InlineData(-10.0)]
    public void Invoice_NegativeAmounts_ShouldThrowArgumentOutOfRangeException(double amount)
    {
        var decimalVal = (decimal)amount;
        var invoice = new Invoice();

        Action setSubTotal = () => invoice.SubTotal = decimalVal;
        Action setTax = () => invoice.TaxAmount = decimalVal;
        Action setDiscount = () => invoice.DiscountAmount = decimalVal;
        Action setTotal = () => invoice.TotalAmount = decimalVal;

        setSubTotal.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("SubTotal");
        setTax.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("TaxAmount");
        setDiscount.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("DiscountAmount");
        setTotal.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("TotalAmount");
    }

    [Fact]
    public void Invoice_DefaultValues_ShouldBeSetCorrectly()
    {
        var invoice = new Invoice();

        invoice.Id.Should().NotBeEmpty();
        invoice.Status.Should().Be(InvoiceStatus.Unpaid);
        invoice.Department.Should().Be("General");
        invoice.IsDeleted.Should().BeFalse();
        invoice.LineItems.Should().NotBeNull().And.BeEmpty();
        invoice.Payments.Should().NotBeNull().And.BeEmpty();
    }
}

