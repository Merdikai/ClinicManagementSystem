using ClinicManagementSystem.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace ClinicManagementSystem.Infrastructure.ExternalServices;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            var emailFrom = _configuration["Email:From"] ?? "clinic@example.com";
            var smtpServer = _configuration["Email:SmtpServer"];
            var portString = _configuration["Email:Port"];
            var username = _configuration["Email:Username"];
            var password = _configuration["Email:Password"];

            if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _logger.LogInformation("[DEV EMAIL MOCK] To: {To} | Subject: {Subject} | Body length: {Length}", to, subject, body.Length);
                return;
            }

            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(emailFrom));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = body };
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            var port = int.TryParse(portString, out var p) ? p : 587;
            await smtp.ConnectAsync(smtpServer, port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(username, password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
        }
    }

    public async Task SendAppointmentConfirmationAsync(string to, string patientName, DateTime appointmentDate, string doctorName)
    {
        var subject = "Appointment Confirmation";
        var body = $@"
            <h2>Appointment Confirmed</h2>
            <p>Dear {patientName},</p>
            <p>Your appointment has been confirmed:</p>
            <ul>
                <li><strong>Date:</strong> {appointmentDate:dddd, MMMM dd, yyyy}</li>
                <li><strong>Time:</strong> {appointmentDate:HH:mm}</li>
                <li><strong>Doctor:</strong> {doctorName}</li>
            </ul>
            <p>Please arrive 15 minutes early.</p>";

        await SendEmailAsync(to, subject, body);
    }

    public async Task SendInvoiceEmailAsync(string to, string invoiceNumber, decimal totalAmount)
    {
        var subject = $"Invoice {invoiceNumber}";
        var body = $@"
            <h2>Invoice</h2>
            <p>Invoice Number: {invoiceNumber}</p>
            <p>Total Amount: {totalAmount:C}</p>
            <p>Thank you for your payment.</p>";

        await SendEmailAsync(to, subject, body);
    }

    public async Task SendPasswordResetAsync(string to, string resetLink)
    {
        var subject = "Password Reset Request";
        var body = $@"
            <h2>Password Reset</h2>
            <p>Click the link below to reset your password:</p>
            <p><a href='{resetLink}'>Reset Password</a></p>
            <p>This link expires in 30 minutes.</p>";

        await SendEmailAsync(to, subject, body);
    }
}
