using ClinicManagementSystem.Application.Common;
using FluentAssertions;
using Xunit;

namespace ClinicManagementSystem.UnitTests.Application.Common;

public class ResultTests
{
    [Fact]
    public void Success_ShouldHoldValueAndBeTrue()
    {
        var result = Result<string>.Success("test payload");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("test payload");
        result.Error.Should().BeNull();
        result.ErrorCode.Should().BeNull();
    }

    [Fact]
    public void Failure_ShouldHoldErrorAndErrorCode()
    {
        var result = Result<int>.Failure("Invalid argument", "invalid_arg");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid argument");
        result.ErrorCode.Should().Be("invalid_arg");
    }

    [Fact]
    public void Match_SuccessBranch_ShouldExecuteOnSuccess()
    {
        var result = Result<int>.Success(42);

        var output = result.Match(
            onSuccess: val => $"Value is {val}",
            onFailure: (err, code) => $"Error: {err}"
        );

        output.Should().Be("Value is 42");
    }

    [Fact]
    public void Match_FailureBranch_ShouldExecuteOnFailure()
    {
        var result = Result<string>.Failure("Resource not found", "not_found");

        var output = result.Match(
            onSuccess: val => $"OK: {val}",
            onFailure: (err, code) => $"Failed with code {code}: {err}"
        );

        output.Should().Be("Failed with code not_found: Resource not found");
    }
}
