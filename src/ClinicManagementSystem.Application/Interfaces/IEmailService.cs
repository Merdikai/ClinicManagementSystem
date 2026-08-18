namespace ClinicManagementSystem.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
    Task SendAppointmentConfirmationAsync(string to, string patientName, DateTime appointmentDate, string doctorName);
    Task SendInvoiceEmailAsync(string to, string invoiceNumber, decimal totalAmount);
    Task SendPasswordResetAsync(string to, string resetLink);
}
