using ClinicManagementSystem.Infrastructure.Identity;
using FluentAssertions;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace ClinicManagementSystem.UnitTests.Identity;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ShouldReturnPbkdf2FormattedString()
    {
        // Arrange
        var password = "SecureClinicalPassword@2026!";

        // Act
        var hash = _hasher.Hash(password);

        // Assert
        hash.Should().StartWith("$pbkdf2-sha256$i=100000$");
    }

    [Fact]
    public void Hash_ShouldProduceUniqueSaltForIdenticalPasswords()
    {
        // Arrange
        var password = "IdenticalDoctorPassword#1";

        // Act
        var hash1 = _hasher.Hash(password);
        var hash2 = _hasher.Hash(password);

        // Assert
        hash1.Should().NotBe(hash2, "Salt must be cryptographically random per invocation");
    }

    [Fact]
    public void Verify_ShouldReturnTrue_ForValidPbkdf2Password()
    {
        // Arrange
        var password = "AttendingPhysician123$!";
        var hash = _hasher.Hash(password);

        // Act
        var isValid = _hasher.Verify(password, hash);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Verify_ShouldReturnFalse_ForInvalidPassword()
    {
        // Arrange
        var correctPassword = "CorrectNursePassword123*";
        var wrongPassword = "WrongNursePassword123*";
        var hash = _hasher.Hash(correctPassword);

        // Act
        var isValid = _hasher.Verify(wrongPassword, hash);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void Verify_ShouldSupportLegacyUnsaltedSha256_ForSeamlessMigration()
    {
        // Arrange
        var legacyPassword = "LegacySeededAdminPass!";
        using var sha256 = SHA256.Create();
        var legacyHashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(legacyPassword));
        var legacyHash = Convert.ToBase64String(legacyHashBytes);

        // Act
        var isValid = _hasher.Verify(legacyPassword, legacyHash);
        var isWrongValid = _hasher.Verify("IncorrectPassword", legacyHash);

        // Assert
        isValid.Should().BeTrue("Legacy unsalted SHA-256 hashes must be verified for existing users");
        isWrongValid.Should().BeFalse();
    }
}

