namespace MyShop.Application.Interfaces;

/// <summary>
/// Interface para envio de emails.
/// Permite abstrair a implementação real de envio de emails,
/// facilitando testes e troca de provedores.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Envia um email.
    /// </summary>
    /// <param name="to">Destinatário</param>
    /// <param name="subject">Assunto</param>
    /// <param name="body">Corpo do email</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}

