using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Dtos;
using MyShop.Application.Services;
using MyShop.Domain;

namespace MyShop.API.Controllers;

/// <summary>
/// Controller para gerenciamento de pedidos.
/// Demonstra operações mais complexas que envolvem múltiplas entidades e regras de negócio.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(OrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Obtém todos os pedidos.
    /// </summary>
    /// <returns>Lista de pedidos</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Order>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrders(CancellationToken cancellationToken)
    {
        // Em um cenário real, aqui usaria um service ou repository diretamente
        // Por simplicidade, vamos retornar uma lista vazia
        // Em uma implementação completa, adicionaríamos um método no service
        return Ok(Array.Empty<Order>());
    }

    /// <summary>
    /// Cria um novo pedido.
    /// </summary>
    /// <param name="dto">Dados do pedido</param>
    /// <returns>Pedido criado</returns>
    [HttpPost]
    [ProducesResponseType(typeof(OrderResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderResultDto>> CreateOrder([FromBody] CreateOrderRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            // Converte o DTO da API para o DTO da aplicação
            var createOrderDto = new CreateOrderDto
            {
                CustomerEmail = dto.CustomerEmail,
                ShippingAddress = new Address(
                    dto.ShippingStreet,
                    dto.ShippingCity,
                    dto.ShippingState,
                    dto.ShippingZipCode,
                    dto.ShippingCountry ?? "Brasil"),
                Items = dto.Items.Select(i => new OrderItemDto
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList()
            };

            var result = await _orderService.CreateOrderAsync(createOrderDto, cancellationToken);
            return CreatedAtAction(nameof(GetOrder), new { id = result.OrderId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém um pedido por ID.
    /// </summary>
    /// <param name="id">ID do pedido</param>
    /// <returns>Pedido encontrado</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Order>> GetOrder(int id, CancellationToken cancellationToken)
    {
        // Em um cenário real, aqui usaria um service ou repository
        // Por simplicidade, retornamos NotFound
        return NotFound($"Order with ID {id} not found");
    }

    /// <summary>
    /// Cancela um pedido.
    /// </summary>
    /// <param name="id">ID do pedido</param>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelOrder(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _orderService.CancelOrderAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order");
            if (ex.Message.Contains("not found"))
                return NotFound(new { error = ex.Message });
            return BadRequest(new { error = ex.Message });
        }
    }
}

/// <summary>
/// DTO para criação de pedido via API.
/// </summary>
public class CreateOrderRequestDto
{
    public string CustomerEmail { get; set; } = string.Empty;
    public string ShippingStreet { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingState { get; set; } = string.Empty;
    public string ShippingZipCode { get; set; } = string.Empty;
    public string? ShippingCountry { get; set; }
    public List<OrderItemRequestDto> Items { get; set; } = new();
}

/// <summary>
/// DTO para item do pedido via API.
/// </summary>
public class OrderItemRequestDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

