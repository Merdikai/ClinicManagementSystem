namespace ClinicManagementSystem.Infrastructure.Caching;

public static class CacheKeys
{
    private const string SchemaVersion = "v1";

    public static string Patient(Guid id) => $"{SchemaVersion}:patient:{id}";
    public static string PatientsAll => $"{SchemaVersion}:patients:all";
    public const string PatientsTag = "patients";

    public static string Medicine(Guid id) => $"{SchemaVersion}:medicine:{id}";
    public static string MedicinesAll => $"{SchemaVersion}:medicines:all";
    public const string MedicinesTag = "medicines";
}
