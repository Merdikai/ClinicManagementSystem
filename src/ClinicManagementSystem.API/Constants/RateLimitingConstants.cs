namespace ClinicManagementSystem.API.Constants;

public static class RateLimitingConstants
{
    public const string AnonymousPolicy = "Anonymous";
    public const string PatientPolicy = "Patient";
    public const string StaffPolicy = "Staff";
    public const string AdminPolicy = "Admin";

    // Token bucket limits
    public const int AnonymousLimit = 10;
    public const int AnonymousWindowSeconds = 10;

    public const int PatientLimit = 50;
    public const int PatientWindowSeconds = 10;

    public const int StaffLimit = 200;
    public const int StaffWindowSeconds = 10;

    public const int AdminLimit = 500;
    public const int AdminWindowSeconds = 10;
}
