namespace ClinicManagementSystem.Application.Exceptions;

public class ConflictException : Exception
{
    public string ConflictReason { get; }

    public ConflictException(string message, string conflictReason = "conflict")
        : base(message)
    {
        ConflictReason = conflictReason;
    }
}
