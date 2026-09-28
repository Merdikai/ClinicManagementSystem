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

    public static string Doctor(Guid id) => $"{SchemaVersion}:doctor:{id}";
    public static string DoctorsAll => $"{SchemaVersion}:doctors:all";
    public const string DoctorsTag = "doctors";

    public static string DoctorSchedule(Guid doctorId, string date) => $"{SchemaVersion}:doctor-schedule:{doctorId}:{date}";
    public const string DoctorSchedulesTag = "doctor_schedules";

    public static string RadiologyOrder(Guid id) => $"{SchemaVersion}:radiology:{id}";
    public static string RadiologyWorklist => $"{SchemaVersion}:radiology:worklist";
    public const string RadiologyTag = "radiology";

    public static string LabOrder(Guid id) => $"{SchemaVersion}:lab-order:{id}";
    public static string LabOrdersPending => $"{SchemaVersion}:lab-orders:pending";
    public const string LabTag = "lab";

    public static string StaffDutyRoster => $"{SchemaVersion}:staff-duty:active";
    public const string StaffDutyTag = "staff_duty";

    public static string Invoice(Guid id) => $"{SchemaVersion}:invoice:{id}";
    public const string InvoicesTag = "invoices";
}
