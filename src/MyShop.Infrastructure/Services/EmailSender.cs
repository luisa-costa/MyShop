using MyShop.Application.Interfaces;

namespace MyShop.Infrastructure.Services;

/// <summary>
/// Implementação real do serviço de envio de emails.
/// Em um cenário real, aqui seria feita a integração com um provedor de email
/// (SendGrid, AWS SES, etc.). Para fins didáticos, apenas simula o envio.
/// </summary>
public class EmailSender : IEmailSender
{
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(ILogger<EmailSender> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        // Em um cenário real, aqui faria a chamada para o provedor de email
        // Por exemplo: await _sendGridClient.SendEmailAsync(...)
        
        // Para fins didáticos, apenas logamos
        _logger.LogInformation("Sending email to {To} with subject: {Subject}", to, subject);
        
        // Simula um delay de rede
        await Task.Delay(100, cancellationToken);
        
        _logger.LogInformation("Email sent successfully to {To}", to);
    }
}

