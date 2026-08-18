namespace ClinicManagementSystem.Application.DTOs;

public record DashboardSummaryDto(
    int TotalPatients,
    int TodayAppointments,
    int PendingAppointments,
    int LowStockMedicines,
    decimal TodayRevenue,
    decimal OutstandingPayments
);
