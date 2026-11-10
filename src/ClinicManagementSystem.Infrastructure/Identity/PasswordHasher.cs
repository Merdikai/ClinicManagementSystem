using System.Security.Cryptography;
using System.Text;
using ClinicManagementSystem.Application.Interfaces;

namespace ClinicManagementSystem.Infrastructure.Identity;

public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16; // 128 bit salt
    private const int KeySize = 32;  // 256 bit subkey
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string Hash(string password)
    {
        return HashPassword(password);
    }

    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            Algorithm,
            KeySize);

        return $"$pbkdf2-sha256$i={Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(passwordHash) || string.IsNullOrEmpty(password))
            return false;

        // PBKDF2-SHA256 Salted Format
        if (passwordHash.StartsWith("$pbkdf2-sha256$"))
        {
            var parts = passwordHash.Split('$');
            // Format: ["", "pbkdf2-sha256", "i=100000", "<salt>", "<hash>"]
            if (parts.Length == 5)
            {
                var iterStr = parts[2].Replace("i=", "");
                if (int.TryParse(iterStr, out var iterations))
                {
                    try
                    {
                        var salt = Convert.FromBase64String(parts[3]);
                        var expectedHash = Convert.FromBase64String(parts[4]);

                        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                            Encoding.UTF8.GetBytes(password),
                            salt,
                            iterations,
                            Algorithm,
                            expectedHash.Length);

                        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
        }

        // Backward compatibility fallback for legacy unsalted SHA-256
        using var sha256 = SHA256.Create();
        var legacyHashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        var legacyHash = Convert.ToBase64String(legacyHashBytes);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(legacyHash),
            Encoding.UTF8.GetBytes(passwordHash));
    }
}
