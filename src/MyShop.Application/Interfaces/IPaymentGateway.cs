using MyShop.Domain;

namespace MyShop.Application.Interfaces;

/// <summary>
/// Interface para gateway de pagamento.
/// Abstrai a integração com serviços de pagamento externos,
/// permitindo mockar em testes e trocar provedores facilmente.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>
    /// Processa um pagamento.
    /// </summary>
    /// <param name="amount">Valor a ser pago</param>
    /// <param name="customerEmail">Email do cliente</param>
    /// <param name="description">Descrição do pagamento</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>ID da transação de pagamento</returns>
    Task<string> ProcessPaymentAsync(Money amount, string customerEmail, string description, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reembolsa um pagamento.
    /// </summary>
    /// <param name="transactionId">ID da transação original</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    Task RefundPaymentAsync(string transactionId, CancellationToken cancellationToken = default);
}

