namespace ClinicManagementSystem.Application.Interfaces;

public record InsuranceVerificationRequest(
    string Provider,
    string PolicyNumber,
    string? CoveragePlan = null,
    string? PatientNationalId = null
);

public record InsuranceVerificationResult(
    bool IsEligible,
    string EligibilityStatus,
    string Provider,
    string PolicyNumber,
    string CoveragePlan,
    decimal CopayPercentage,
    decimal AnnualRemainingCap,
    string VerificationReference,
    DateTime VerifiedAtUtc,
    string Message,
    bool UsedClearinghouseHttp = false
);

public interface IInsuranceVerificationClient
{
    Task<InsuranceVerificationResult> VerifyEligibilityAsync(
        InsuranceVerificationRequest request,
        CancellationToken ct = default);
}

