namespace OmniRoute.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendOtpEmailAsync(string email, string otp, CancellationToken cancellationToken = default);
    Task SendNotificationEmailAsync(string email, string subject, string message, CancellationToken cancellationToken = default);
    Task SendWelcomeEmailAsync(string email, string username, string tempPassword, CancellationToken cancellationToken = default);
}

