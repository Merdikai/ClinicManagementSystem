namespace ClinicManagementSystem.Application.Exceptions;

public class BusinessRuleViolationException : Exception
{
    public string ErrorCode { get; }

    public BusinessRuleViolationException(string message, string errorCode = "business_rule_violation")
        : base(message)
    {
        ErrorCode = errorCode;
    }
}