using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Dtos;
using MyShop.Application.Services;
using MyShop.Domain;

namespace MyShop.API.Controllers;

/// <summary>
/// Controller para gerenciamento de produtos.
/// Demonstra operações CRUD básicas.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(ProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Obtém todos os produtos ativos.
    /// </summary>
    /// <returns>Lista de produtos ativos</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Product>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts(CancellationToken cancellationToken)
    {
        var products = await _productService.GetActiveProductsAsync(cancellationToken);
        return Ok(products);
    }

    /// <summary>
    /// Obtém um produto por ID.
    /// </summary>
    /// <param name="id">ID do produto</param>
    /// <returns>Produto encontrado</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Product>> GetProduct(int id, CancellationToken cancellationToken)
    {
        var product = await _productService.GetProductByIdAsync(id, cancellationToken);
        if (product == null)
            return NotFound($"Product with ID {id} not found");

        return Ok(product);
    }

    /// <summary>
    /// Cria um novo produto.
    /// </summary>
    /// <param name="request">Dados do produto</param>
    /// <returns>Produto criado</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Product), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Product>> CreateProduct([FromBody] CreateProductDto request, CancellationToken cancellationToken)
    {
        try
        {
            var price = new Money(request.Price, request.Currency ?? "BRL");
            var product = await _productService.CreateProductAsync(
                request.Name,
                request.Description ?? string.Empty,
                price,
                request.StockQuantity,
                cancellationToken);

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Atualiza o estoque de um produto.
    /// </summary>
    /// <param name="id">ID do produto</param>
    /// <param name="request">Nova quantidade em estoque</param>
    [HttpPut("{id}/stock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockDto request, CancellationToken cancellationToken)
    {
        try
        {
            await _productService.UpdateProductStockAsync(id, request.StockQuantity, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product stock");
            if (ex.Message.Contains("not found"))
                return NotFound(ex.Message);
            return BadRequest(ex.Message);
        }
    }
}

/// <summary>
/// DTO para criação de produto.
/// </summary>
public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? Currency { get; set; }
    public int StockQuantity { get; set; }
}

/// <summary>
/// DTO para atualização de estoque.
/// </summary>
public class UpdateStockDto
{
    public int StockQuantity { get; set; }
}

