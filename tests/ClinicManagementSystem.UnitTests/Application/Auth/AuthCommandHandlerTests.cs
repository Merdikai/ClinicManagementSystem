using ClinicManagementSystem.Application.Auth.Commands;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Options;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using ClinicManagementSystem.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace ClinicManagementSystem.UnitTests.Application.Auth;

public class AuthCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IJwtTokenGenerator _tokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IClinicDbContext _context = Substitute.For<IClinicDbContext>();
    private readonly IOptions<JwtOptions> _jwtOptions;

    public AuthCommandHandlerTests()
    {
        _jwtOptions = Options.Create(new JwtOptions
        {
            Key = "TestSuperSecretKeyThatIsAtLeast32CharactersLong!",
            Issuer = "ClinicTest",
            Audience = "ClinicTest",
            ExpiryMinutes = 15,
            RefreshTokenExpiryDays = 7
        });
    }

    [Fact]
    public async Task Login_ValidCredentials_ShouldResetFailuresAndReturnAuthResponse()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "doctor.smith",
            Email = "smith@clinic.com",
            PasswordHash = "hashed-pass",
            FirstName = "John",
            LastName = "Smith",
            IsActive = true,
            FailedLoginAttempts = 2
        };

        _userRepository.GetByUsernameAsync("doctor.smith").Returns(user);
        _passwordHasher.Verify("ValidPassword123!", "hashed-pass").Returns(true);
        _tokenGenerator.GenerateToken(user, Arg.Any<IList<string>>()).Returns("jwt-token-xyz");

        var refreshTokensDbSet = Substitute.For<DbSet<RefreshToken>>();
        _context.RefreshTokens.Returns(refreshTokensDbSet);

        var patientsDbSet = new List<Patient>().BuildMockDbSet();
        _context.Patients.Returns(patientsDbSet);

        var handler = new LoginCommandHandler(_userRepository, _tokenGenerator, _passwordHasher, _context, _jwtOptions);
        var result = await handler.Handle(new LoginCommand("doctor.smith", "ValidPassword123!"), CancellationToken.None);

        result.Should().NotBeNull();
        result.Token.Should().Be("jwt-token-xyz");
        result.Username.Should().Be("doctor.smith");
        user.FailedLoginAttempts.Should().Be(0);
        user.LockoutEndUtc.Should().BeNull();
    }

    [Fact]
    public async Task Login_InvalidPassword_ShouldIncrementFailuresAndThrow()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "nurse.joy",
            PasswordHash = "correct-hash",
            IsActive = true,
            FailedLoginAttempts = 1
        };

        _userRepository.GetByUsernameAsync("nurse.joy").Returns(user);
        _passwordHasher.Verify("WrongPassword!", "correct-hash").Returns(false);

        var handler = new LoginCommandHandler(_userRepository, _tokenGenerator, _passwordHasher, _context, _jwtOptions);
        var act = async () => await handler.Handle(new LoginCommand("nurse.joy", "WrongPassword!"), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("*Invalid username or password*");

        user.FailedLoginAttempts.Should().Be(2);
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_FifthFailedAttempt_ShouldLockAccountFor15Minutes()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "target.user",
            PasswordHash = "correct-hash",
            IsActive = true,
            FailedLoginAttempts = 4
        };

        _userRepository.GetByUsernameAsync("target.user").Returns(user);
        _passwordHasher.Verify("WrongPassword!", "correct-hash").Returns(false);

        var handler = new LoginCommandHandler(_userRepository, _tokenGenerator, _passwordHasher, _context, _jwtOptions);
        var act = async () => await handler.Handle(new LoginCommand("target.user", "WrongPassword!"), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleViolationException>();

        user.FailedLoginAttempts.Should().Be(0);
        user.LockoutEndUtc.Should().NotBeNull();
        user.LockoutEndUtc!.Value.Should().BeAfter(DateTime.UtcNow.AddMinutes(14));
    }

    [Fact]
    public async Task Login_WhenAccountIsLocked_ShouldThrowWithoutCheckingPassword()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "locked.user",
            PasswordHash = "hash",
            IsActive = true,
            LockoutEndUtc = DateTime.UtcNow.AddMinutes(10)
        };

        _userRepository.GetByUsernameAsync("locked.user").Returns(user);

        var handler = new LoginCommandHandler(_userRepository, _tokenGenerator, _passwordHasher, _context, _jwtOptions);
        var act = async () => await handler.Handle(new LoginCommand("locked.user", "AnyPassword"), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("*locked*");

        _passwordHasher.DidNotReceive().Verify(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Login_DeactivatedUser_ShouldThrowAccountDeactivated()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "deactivated.user",
            PasswordHash = "hash",
            IsActive = false
        };

        _userRepository.GetByUsernameAsync("deactivated.user").Returns(user);
        _passwordHasher.Verify("Pass123!", "hash").Returns(true);

        var handler = new LoginCommandHandler(_userRepository, _tokenGenerator, _passwordHasher, _context, _jwtOptions);
        var act = async () => await handler.Handle(new LoginCommand("deactivated.user", "Pass123!"), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("*deactivated*");
    }
}

