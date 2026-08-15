namespace PersonaNest.Services.Interfaces;

/// <summary>
/// Sends transactional email (§12/D-17). The only implementation is
/// <c>DevelopmentEmailSender</c>, which logs instead of sending real mail - there is no
/// production SMTP configuration in scope for this project.
/// </summary>
public interface IEmailSender
{
    Task SendPasswordResetEmailAsync(
        string toEmail, string resetLink, CancellationToken cancellationToken = default);
}
