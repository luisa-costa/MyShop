using FluentAssertions;
using MyShop.Application.Interfaces;
using MyShop.Application.Services;
using MyShop.Domain;
using MyShop.Domain.Exceptions;
using Moq;

namespace MyShop.UnitTests.Services;

/// <summary>
/// Testes unitários para ProductService.
/// 
/// Estes testes demonstram:
/// - Padrão AAA (Arrange, Act, Assert)
/// - Testes de lógica de negócio
/// - Testes de casos de sucesso e falha
/// - Uso de mocks para isolar dependências
/// </summary>
public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _mockRepository;
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        // Arrange: Configuração inicial comum a todos os testes
        _mockRepository = new Mock<IProductRepository>();
        _service = new ProductService(_mockRepository.Object);
    }

    [Fact]
    public async Task GetProductByIdAsync_WhenProductExists_ShouldReturnProduct()
    {
        // Arrange: Prepara os dados e configurações necessárias
        var productId = 1;
        var expectedProduct = new Product("Test Product", "Description", new Money(100.00m), 10);
        
        // Configura o mock para retornar o produto quando GetByIdAsync for chamado
        _mockRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedProduct);

        // Act: Executa a ação que está sendo testada
        var result = await _service.GetProductByIdAsync(productId);

        // Assert: Verifica se o resultado está correto
        result.Should().NotBeNull();
        result.Should().Be(expectedProduct);
        result!.Name.Should().Be("Test Product");
        
        // Verifica se o método do repositório foi chamado corretamente
        _mockRepository.Verify(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProductByIdAsync_WhenProductDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var productId = 999;
        _mockRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _service.GetProductByIdAsync(productId);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetActiveProductsAsync_ShouldReturnOnlyActiveProducts()
    {
        // Arrange
        var activeProduct1 = new Product("Active 1", "Desc", new Money(10m), 5);
        var activeProduct2 = new Product("Active 2", "Desc", new Money(20m), 3);
        var inactiveProduct = new Product("Inactive", "Desc", new Money(30m), 0);
        inactiveProduct.Deactivate();

        var allProducts = new List<Product> { activeProduct1, activeProduct2, inactiveProduct };

        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allProducts);

        // Act
        var result = await _service.GetActiveProductsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.IsActive);
        result.Should().Contain(activeProduct1);
        result.Should().Contain(activeProduct2);
        result.Should().NotContain(inactiveProduct);
    }

    [Fact]
    public async Task CreateProductAsync_WithValidData_ShouldCreateProduct()
    {
        // Arrange
        var name = "New Product";
        var description = "Product Description";
        var price = new Money(99.99m);
        var stockQuantity = 50;

        Product? savedProduct = null;
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken ct) =>
            {
                savedProduct = p;
                return p;
            });

        // Act
        var result = await _service.CreateProductAsync(name, description, price, stockQuantity);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(name);
        result.Description.Should().Be(description);
        result.Price.Should().Be(price);
        result.StockQuantity.Should().Be(stockQuantity);
        result.IsActive.Should().BeTrue();
        
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task CreateProductAsync_WithInvalidName_ShouldThrowDomainException(string? invalidName)
    {
        // Arrange
        var price = new Money(100m);
        var stockQuantity = 10;

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(async () =>
            await _service.CreateProductAsync(invalidName!, "Description", price, stockQuantity));
        
        // Verifica que o repositório NÃO foi chamado (a validação falhou antes)
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateProductAsync_WithZeroPrice_ShouldThrowDomainException()
    {
        // Arrange
        var price = new Money(0m);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(async () =>
            await _service.CreateProductAsync("Product", "Description", price, 10));
    }

    [Fact]
    public async Task CreateProductAsync_WithNegativeStock_ShouldThrowDomainException()
    {
        // Arrange
        var price = new Money(100m);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(async () =>
            await _service.CreateProductAsync("Product", "Description", price, -1));
    }

    [Fact]
    public async Task UpdateProductStockAsync_WhenProductExists_ShouldUpdateStock()
    {
        // Arrange
        var productId = 1;
        var existingProduct = new Product("Product", "Desc", new Money(100m), 10);
        var newStockQuantity = 25;

        _mockRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act
        await _service.UpdateProductStockAsync(productId, newStockQuantity);

        // Assert
        existingProduct.StockQuantity.Should().Be(newStockQuantity);
        _mockRepository.Verify(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(existingProduct, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProductStockAsync_WhenProductDoesNotExist_ShouldThrowDomainException()
    {
        // Arrange
        var productId = 999;
        _mockRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(async () =>
            await _service.UpdateProductStockAsync(productId, 10));
        
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProductStockAsync_WithNegativeStock_ShouldThrowDomainException()
    {
        // Arrange
        var productId = 1;
        var existingProduct = new Product("Product", "Desc", new Money(100m), 10);

        _mockRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(async () =>
            await _service.UpdateProductStockAsync(productId, -5));
    }
}

