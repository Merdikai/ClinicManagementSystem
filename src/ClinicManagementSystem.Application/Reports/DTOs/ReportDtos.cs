namespace ClinicManagementSystem.Application.Reports.DTOs;

public record DailyRevenueReportDto(
    DateTime Date,
    int TotalPatients,
    int TotalAppointments,
    decimal TotalRevenue,
    decimal TotalTax,
    decimal TotalDiscounts
);

public record TopMedicineDto(
    string MedicineName,
    string MedicineCode,
    int TotalQuantity,
    decimal TotalRevenue
);

public record DoctorAppointmentCountDto(
    string DoctorName,
    int AppointmentCount,
    int CompletedCount
);
