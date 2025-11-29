using Microsoft.Extensions.Logging;
using MyShop.Application.Interfaces;
using MyShop.Domain;

namespace MyShop.Infrastructure.Services;

/// <summary>
/// Implementação real do gateway de pagamento.
/// Em um cenário real, aqui seria feita a integração com um provedor de pagamento
/// (Stripe, PayPal, etc.). Para fins didáticos, apenas simula o processamento.
/// </summary>
public class PaymentGateway : IPaymentGateway
{
    private readonly ILogger<PaymentGateway> _logger;
    private static int _transactionCounter = 1;

    public PaymentGateway(ILogger<PaymentGateway> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> ProcessPaymentAsync(Money amount, string customerEmail, string description, CancellationToken cancellationToken = default)
    {
        // Em um cenário real, aqui faria a chamada para o gateway de pagamento
        // Por exemplo: await _stripeClient.ChargeAsync(...)
        
        // Para fins didáticos, apenas geramos um ID de transação simulado
        var transactionId = $"TXN-{DateTime.UtcNow:yyyyMMdd}-{_transactionCounter++:D6}";
        
        _logger.LogInformation(
            "Processing payment: {Amount} for {Email}. Description: {Description}. Transaction ID: {TransactionId}",
            amount, customerEmail, description, transactionId);
        
        // Simula um delay de rede
        await Task.Delay(200, cancellationToken);
        
        _logger.LogInformation("Payment processed successfully. Transaction ID: {TransactionId}", transactionId);
        
        return transactionId;
    }

    public async Task RefundPaymentAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        // Em um cenário real, aqui faria a chamada para reembolsar
        // Por exemplo: await _stripeClient.RefundAsync(transactionId)
        
        _logger.LogInformation("Refunding payment. Transaction ID: {TransactionId}", transactionId);
        
        // Simula um delay de rede
        await Task.Delay(200, cancellationToken);
        
        _logger.LogInformation("Payment refunded successfully. Transaction ID: {TransactionId}", transactionId);
    }
}

