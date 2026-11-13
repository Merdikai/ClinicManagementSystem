namespace ClinicManagementSystem.Application.Exceptions;

public class SecurityAlertException : Exception
{
    public string Code { get; }

    public SecurityAlertException(string message, string code = "security_alert")
        : base(message)
    {
        Code = code;
    }

    public SecurityAlertException(string message, Exception innerException, string code = "security_alert")
        : base(message, innerException)
    {
        Code = code;
    }
}

