namespace ClinicManagementSystem.Application.Exceptions;

public class SecurityAlertException : Exception
{
    public string Code { get; }

    public SecurityAlertException(string message, string code = "security_alert")
        : base(message)
    {
        Code = code;
    }
}

