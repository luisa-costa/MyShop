# Exercícios - Módulo 2: Testes Unitários Avançados

## Objetivos de Aprendizagem
- Testar services e lógica de negócio
- Usar Moq para criar mocks e isolar dependências
- Utilizar o Test Explorer eficientemente
- Debugar testes
- Entender e configurar paralelismo
- Executar testes via linha de comando

---

## 1. Testando Services (Lógica de Negócio)

### Por que testar Services?

Services contêm a lógica de negócio da aplicação. Testar services garante que:
- As regras de negócio funcionam corretamente
- As validações estão sendo aplicadas
- As operações retornam os resultados esperados

### Exercício 2.1: Analisar um teste de service existente

**Tarefa**: Abra [ProductServiceTests.cs](../tests/MyShop.UnitTests/Services/ProductServiceTests.cs) e analise o teste `GetProductByIdAsync_WhenProductExists_ShouldReturnProduct` (linha 22).

**Questões para reflexão**:
1. Quais dependências o `ProductService` possui?
2. Como essas dependências são fornecidas no teste?
3. O que está sendo testado: o service ou o repositório?

**Resposta esperada**:
- O service depende de `IProductRepository`
- Um mock do repositório é criado com Moq
- Apenas o service está sendo testado (o repositório é "fake")

---

## 2. Introdução ao Mocking com Moq

### O que é Mocking?

**Mock** é um objeto falso que simula o comportamento de uma dependência real. Usamos mocks para:
- Isolar a classe sendo testada
- Controlar o comportamento das dependências
- Verificar se métodos foram chamados corretamente

### Exercício 2.2: Criar seu primeiro mock

**Tarefa**: Crie um arquivo `OrderServiceTests.cs` em `tests/MyShop.UnitTests/Services/`:

```csharp
using Moq;
using MyShop.Application.Interfaces;
using MyShop.Application.Services;
using MyShop.Domain;

namespace MyShop.UnitTests.Services;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly OrderService _orderService;

    public OrderServiceTests()
    {
        // Arrange: Configuração inicial comum a todos os testes
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockProductRepository = new Mock<IProductRepository>();
        _orderService = new OrderService(_mockOrderRepository.Object, _mockProductRepository.Object);
    }

    [Fact]
    public async Task GetOrderByIdAsync_WhenOrderExists_ShouldReturnOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var customerEmail = "customer@test.com";
        var shippingAddress = new Address("Rua Teste", "São Paulo", "SP", "01234-567", "Brasil");
        var expectedOrder = new Order(customerEmail, shippingAddress);

        // Configura o mock para retornar a order
        _mockOrderRepository
            .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOrder);

        // Act
        var result = await _orderService.GetOrderByIdAsync(orderId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedOrder, result);

        // Verifica que o método foi chamado exatamente uma vez
        _mockOrderRepository.Verify(
            r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
```

**Conceitos do Moq**:
- `new Mock<T>()` - Cria um mock da interface
- `.Setup()` - Configura o comportamento do mock
- `.ReturnsAsync()` - Define o retorno de método assíncrono
- `.Verify()` - Verifica se método foi chamado
- `Times.Once` - Verifica que foi chamado exatamente uma vez

---

## 3. Setup e Configuração de Mocks

### Exercício 2.3: Diferentes formas de configurar mocks

**Tarefa**: Adicione os seguintes testes para explorar diferentes configurações:

```csharp
[Fact]
public async Task GetOrderByIdAsync_WhenOrderDoesNotExist_ShouldReturnNull()
{
    // Arrange
    var orderId = Guid.NewGuid();

    _mockOrderRepository
        .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
        .ReturnsAsync((Order?)null);

    // Act
    var result = await _orderService.GetOrderByIdAsync(orderId);

    // Assert
    Assert.Null(result);
}

[Fact]
public async Task GetOrderByIdAsync_WhenRepositoryThrows_ShouldPropagateException()
{
    // Arrange
    var orderId = Guid.NewGuid();

    _mockOrderRepository
        .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
        .ThrowsAsync(new InvalidOperationException("Database error"));

    // Act & Assert
    var exception = await Assert.ThrowsAsync<InvalidOperationException>(
        () => _orderService.GetOrderByIdAsync(orderId)
    );

    Assert.Equal("Database error", exception.Message);
}
```

**Métodos de configuração do Moq**:
- `.Returns(value)` - Retorna valor síncrono
- `.ReturnsAsync(value)` - Retorna valor assíncrono
- `.Throws<TException>()` - Lança exceção síncrona
- `.ThrowsAsync<TException>()` - Lança exceção assíncrona
- `.Callback(action)` - Executa ação quando método é chamado

---

## 4. It.IsAny e Matchers

### Exercício 2.4: Usar matchers para configuração flexível

**Tarefa**: Crie testes que usam diferentes matchers:

```csharp
[Fact]
public async Task GetProductsByPriceRange_ShouldUseCorrectParameters()
{
    // Arrange
    var products = new List<Product>
    {
        new Product("Product 1", "Desc", new Money(50m), 10),
        new Product("Product 2", "Desc", new Money(100m), 5)
    };

    _mockProductRepository
        .Setup(r => r.GetByPriceRangeAsync(
            It.Is<decimal>(min => min >= 0),           // Qualquer valor >= 0
            It.Is<decimal>(max => max > 0),            // Qualquer valor > 0
            It.IsAny<CancellationToken>()              // Qualquer CancellationToken
        ))
        .ReturnsAsync(products);

    // Act
    var result = await _productService.GetProductsByPriceRangeAsync(10m, 200m);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(2, result.Count());
}

[Fact]
public async Task CreateProduct_ShouldCallRepositoryWithCorrectProduct()
{
    // Arrange
    Product? capturedProduct = null;

    _mockProductRepository
        .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync((Product p, CancellationToken ct) =>
        {
            capturedProduct = p;  // Captura o produto passado
            return p;
        });

    // Act
    await _productService.CreateProductAsync("Test", "Desc", new Money(100m), 10);

    // Assert
    Assert.NotNull(capturedProduct);
    Assert.Equal("Test", capturedProduct.Name);
}
```

**Matchers do Moq**:
- `It.IsAny<T>()` - Aceita qualquer valor do tipo T
- `It.Is<T>(predicate)` - Aceita valores que satisfazem o predicado
- `It.IsNotNull<T>()` - Aceita qualquer valor não nulo
- `It.IsRegex(pattern)` - Aceita strings que correspondem ao regex

---

## 5. Verify: Verificando Chamadas

### Exercício 2.5: Verificar comportamento dos mocks

**Tarefa**: Analise o teste em [ProductServiceTests.cs:199](../tests/MyShop.UnitTests/Services/ProductServiceTests.cs#L199) e crie testes similares:

```csharp
[Fact]
public async Task UpdateProductStock_ShouldCallGetByIdAndUpdate()
{
    // Arrange
    var productId = Guid.NewGuid();
    var product = new Product("Product", "Desc", new Money(100m), 10);

    _mockProductRepository
        .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(product);

    // Act
    await _productService.UpdateProductStockAsync(productId, 25);

    // Assert: Verifica sequência de chamadas
    _mockProductRepository.Verify(
        r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()),
        Times.Once
    );

    _mockProductRepository.Verify(
        r => r.UpdateAsync(product, It.IsAny<CancellationToken>()),
        Times.Once
    );

    // Verifica que nenhum outro método foi chamado
    _mockProductRepository.VerifyNoOtherCalls();
}

[Fact]
public async Task CreateProduct_WhenValidationFails_ShouldNotCallRepository()
{
    // Act & Assert
    await Assert.ThrowsAsync<DomainException>(
        () => _productService.CreateProductAsync("", "Desc", new Money(100m), 10)
    );

    // Verifica que AddAsync NUNCA foi chamado
    _mockProductRepository.Verify(
        r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
        Times.Never
    );
}
```

**Verificadores do Moq**:
- `Times.Once` - Exatamente uma vez
- `Times.Never` - Nunca foi chamado
- `Times.AtLeastOnce` - Pelo menos uma vez
- `Times.Exactly(n)` - Exatamente n vezes
- `Times.AtLeast(n)` - Pelo menos n vezes
- `Times.AtMost(n)` - No máximo n vezes

---

## 6. Test Explorer

### Exercício 2.6: Explorar o Test Explorer

**Tarefa**: No Visual Studio, abra o Test Explorer (`Test` → `Test Explorer` ou `Ctrl+E, T`).

**Ações**:
1. ✅ Visualize todos os testes do projeto
2. ✅ Agrupe testes por Namespace
3. ✅ Agrupe testes por Class
4. ✅ Agrupe testes por Trait (Category)
5. ✅ Filtre testes por nome (ex: "Product")

**Dica**: Você pode usar Traits para categorizar testes:
```csharp
[Fact]
[Trait("Category", "Product")]
[Trait("Priority", "High")]
public async Task ImportantProductTest()
{
    // ...
}
```

---

## 7. Executar Um ou Mais Testes

### Exercício 2.7: Executar testes seletivamente

**Via Test Explorer**:
- ✅ Clique com botão direito em um teste → "Run"
- ✅ Selecione múltiplos testes (Ctrl+Click) → "Run"
- ✅ Clique em uma classe → "Run" (executa todos da classe)

**Via linha de comando**:

```bash
# Executar todos os testes
dotnet test

# Executar testes de um projeto específico
dotnet test tests/MyShop.UnitTests

# Executar apenas testes que contém "Product" no nome
dotnet test --filter "FullyQualifiedName~Product"

# Executar testes de uma classe específica
dotnet test --filter "FullyQualifiedName~ProductServiceTests"

# Executar um teste específico
dotnet test --filter "FullyQualifiedName~ProductServiceTests.GetProductByIdAsync_WhenProductExists_ShouldReturnProduct"

# Executar testes por categoria (Trait)
dotnet test --filter "Category=Product"

# Executar testes por prioridade
dotnet test --filter "Priority=High"
```

---

## 8. Debugar um Teste

### Exercício 2.8: Debugar teste com breakpoint

**Tarefa**:
1. Abra [ProductServiceTests.cs](../tests/MyShop.UnitTests/Services/ProductServiceTests.cs)
2. Coloque um breakpoint na linha 34 (dentro do método `GetProductByIdAsync_WhenProductExists_ShouldReturnProduct`)
3. Clique com botão direito no teste → "Debug"
4. Quando o debugger pausar, inspecione as variáveis:
   - Veja o valor de `productId`
   - Veja o estado de `expectedProduct`
   - Use o Watch window para avaliar `result.Name`
5. Continue a execução (F5)

**Via linha de comando com debugger**:
```bash
# Anexe o debugger do VS Code ou Visual Studio
dotnet test --filter "FullyQualifiedName~GetProductByIdAsync_WhenProductExists" --logger "console;verbosity=detailed"
```

---

## 9. Paralelismo dos Testes

### Por que paralelismo importa?

Por padrão, xUnit executa testes em paralelo para melhorar performance. Porém, alguns testes precisam ser executados sequencialmente.

### Exercício 2.9: Entender paralelismo do xUnit

**Tarefa**: Analise o comportamento padrão do xUnit:

```csharp
public class ParallelismTests
{
    [Fact]
    public async Task Test1_RunsInParallel()
    {
        await Task.Delay(1000);
        var threadId = Environment.CurrentManagedThreadId;
        Console.WriteLine($"Test1 - Thread: {threadId}");
    }

    [Fact]
    public async Task Test2_RunsInParallel()
    {
        await Task.Delay(1000);
        var threadId = Environment.CurrentManagedThreadId;
        Console.WriteLine($"Test2 - Thread: {threadId}");
    }

    [Fact]
    public async Task Test3_RunsInParallel()
    {
        await Task.Delay(1000);
        var threadId = Environment.CurrentManagedThreadId;
        Console.WriteLine($"Test3 - Thread: {threadId}");
    }
}
```

**Execute**:
```bash
dotnet test --filter "FullyQualifiedName~ParallelismTests" --logger "console;verbosity=detailed"
```

**Observe**: Os testes rodam ao mesmo tempo (threads diferentes).

### Exercício 2.10: Desabilitar paralelismo quando necessário

**Tarefa**: Veja o exemplo em [EfCoreInMemoryIntegrationTests.cs:21](../tests/MyShop.IntegrationTests/EfCoreInMemoryIntegrationTests.cs#L21):

```csharp
// Desabilita paralelismo para uma collection
[CollectionDefinition("Integration Tests", DisableParallelization = true)]
public class IntegrationTestCollection : ICollectionFixture<DatabaseFixture>
{
}

// Testes desta collection rodam sequencialmente
[Collection("Integration Tests")]
public class EfCoreInMemoryIntegrationTests
{
    // ...
}
```

**Quando desabilitar paralelismo**:
- Testes de integração com banco de dados compartilhado
- Testes que manipulam arquivos ou recursos compartilhados
- Testes que dependem de estado global

### Exercício 2.11: Configurar paralelismo via assembly

**Tarefa**: Crie um arquivo `AssemblyInfo.cs` em `tests/MyShop.UnitTests/`:

```csharp
using Xunit;

// Limita execução paralela a 4 threads
[assembly: CollectionBehavior(MaxParallelThreads = 4)]

// OU desabilita paralelismo completamente
// [assembly: CollectionBehavior(DisableTestParallelization = true)]
```

---

## 10. Executar via Linha de Comando

### Exercício 2.12: Comandos úteis do dotnet test

**Tarefa**: Execute os seguintes comandos e observe os resultados:

```bash
# 1. Executar todos os testes com saída detalhada
dotnet test --logger "console;verbosity=detailed"

# 2. Executar apenas testes unitários
dotnet test tests/MyShop.UnitTests

# 3. Executar com configuração Release (mais rápido)
dotnet test --configuration Release

# 4. Executar sem build (se já buildou antes)
dotnet test --no-build

# 5. Executar em paralelo com máximo de 8 processos
dotnet test --parallel --max-cpu-count 8

# 6. Executar e coletar cobertura de código
dotnet test /p:CollectCoverage=true

# 7. Executar testes que falharam na última execução
dotnet test --filter "FullyQualifiedName~FailedTest"

# 8. Executar com timeout de 30 segundos por teste
dotnet test --blame-hang-timeout 30s

# 9. Executar e gerar relatório TRX
dotnet test --logger "trx;LogFileName=test-results.trx"

# 10. Executar múltiplos filtros
dotnet test --filter "FullyQualifiedName~Product&Category=Integration"
```

---

## 11. Desafio Final do Módulo 2

### Exercício 2.13: Criar testes completos para OrderService

**Tarefa**: Implemente uma suíte completa de testes para `OrderService` que cubra:

1. ✅ `GetOrderByIdAsync` - quando pedido existe
2. ✅ `GetOrderByIdAsync` - quando pedido não existe
3. ✅ `CreateOrderAsync` - com dados válidos (use mock para produtos)
4. ✅ `CreateOrderAsync` - quando produto não tem estoque suficiente
5. ✅ `AddItemToOrderAsync` - adiciona item com sucesso
6. ✅ `AddItemToOrderAsync` - falha quando pedido não existe
7. ✅ Use `Verify` para garantir que os métodos corretos do repositório foram chamados
8. ✅ Use `Times.Never` para validar que repositório não foi chamado em casos de erro

**Dicas**:
- Crie mocks para `IOrderRepository` e `IProductRepository`
- Use `It.IsAny<>` e `It.Is<>` para matchers flexíveis
- Consulte [OrderServiceTests.cs](../tests/MyShop.UnitTests/Services/OrderServiceTests.cs) como referência

**Bônus**:
- Adicione traits `[Trait("Category", "Order")]` nos testes
- Execute apenas esses testes: `dotnet test --filter "Category=Order"`
- Debug um dos testes e inspecione o estado dos mocks

---

## Checklist de Conclusão

Ao finalizar este módulo, você deve ser capaz de:

- [ ] Criar mocks com Moq usando `new Mock<T>()`
- [ ] Configurar comportamento de mocks com `.Setup()`, `.Returns()`, `.Throws()`
- [ ] Usar matchers: `It.IsAny<T>()`, `It.Is<T>(predicate)`
- [ ] Verificar chamadas com `.Verify()` e `Times`
- [ ] Usar o Test Explorer para executar e organizar testes
- [ ] Debugar testes com breakpoints
- [ ] Entender e configurar paralelismo com Collections
- [ ] Executar testes via linha de comando com filtros

---

## Próximos Passos

No próximo módulo, você aprenderá sobre:
- Testes de integração com banco em memória
- Testes de APIs
- Geração de relatório de cobertura de testes

Consulte: [exercicios-03-testes-integracao-cobertura.md](./exercicios-03-testes-integracao-cobertura.md)
