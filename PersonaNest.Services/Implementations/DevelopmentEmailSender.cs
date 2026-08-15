using Microsoft.Extensions.Logging;
using PersonaNest.Services.Interfaces;

namespace PersonaNest.Services.Implementations;

/// <inheritdoc cref="IEmailSender"/>
/// <summary>
/// Logs the reset link via Serilog instead of sending real email (§12/D-17, §24). This is the
/// only <see cref="IEmailSender"/> the project registers - there is no production sender.
/// </summary>
public class DevelopmentEmailSender : IEmailSender
{
    private readonly ILogger<DevelopmentEmailSender> _logger;

    public DevelopmentEmailSender(ILogger<DevelopmentEmailSender> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task SendPasswordResetEmailAsync(
        string toEmail, string resetLink, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[DEV EMAIL] Password reset requested for {Email}. Reset link: {ResetLink}",
            toEmail, resetLink);

        return Task.CompletedTask;
    }
}
