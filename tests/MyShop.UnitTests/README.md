# MyShop.UnitTests

Este projeto contém os testes unitários do MyShop, demonstrando testes de lógica de negócio, mocking com Moq e configuração de paralelismo.

## 📁 Estrutura

```
MyShop.UnitTests/
├── Services/
│   ├── ProductServiceTests.cs      # Testes de lógica de negócio de produtos
│   └── OrderServiceTests.cs        # Testes de lógica de negócio de pedidos
├── Mocking/
│   └── OrderServiceWithMocksTests.cs  # Exemplos avançados de Moq
├── Collections/
│   ├── ParallelTestCollection.cs      # Collection para testes paralelos
│   ├── ParallelTests.cs               # Testes que podem rodar em paralelo
│   ├── SequentialTestCollection.cs    # Collection para testes sequenciais
│   └── SequentialTests.cs              # Testes que NÃO podem rodar em paralelo
└── AssemblyInfo.cs                     # Configuração global de paralelismo
```

## 🧪 Testes de Services

### ProductServiceTests.cs

Demonstra testes unitários focados em lógica de negócio usando o padrão AAA (Arrange, Act, Assert).

**Exemplo:**
```csharp
[Fact]
public async Task GetProductByIdAsync_WhenProductExists_ShouldReturnProduct()
{
    // Arrange: Prepara os dados
    var productId = 1;
    var expectedProduct = new Product(...);
    _mockRepository.Setup(...).ReturnsAsync(expectedProduct);

    // Act: Executa a ação
    var result = await _service.GetProductByIdAsync(productId);

    // Assert: Verifica o resultado
    result.Should().NotBeNull();
    result.Name.Should().Be("Test Product");
}
```

**Conceitos demonstrados:**
- Padrão AAA
- Testes de casos de sucesso e falha
- Uso de mocks para isolar dependências
- Validações de regras de negócio

### OrderServiceTests.cs

Testa regras de negócio complexas como cálculo de frete, descontos e validações de estoque.

**Regras de negócio testadas:**
- Frete grátis para pedidos acima de R$ 200
- Desconto de 10% para pedidos acima de R$ 500
- Validação de estoque suficiente

## 🎭 Mocking com Moq

### OrderServiceWithMocksTests.cs

Demonstra o uso avançado de Moq para mockar dependências e verificar interações.

**Conceitos demonstrados:**

#### 1. Setup de Métodos
```csharp
_mockPaymentGateway
    .Setup(g => g.ProcessPaymentAsync(It.IsAny<Money>(), ...))
    .ReturnsAsync("TXN-123");
```

#### 2. Verificação de Chamadas
```csharp
_mockPaymentGateway.Verify(
    g => g.ProcessPaymentAsync(It.IsAny<Money>(), ...),
    Times.Once);
```

#### 3. Verificação com Predicados
```csharp
_mockPaymentGateway.Verify(
    g => g.ProcessPaymentAsync(
        It.Is<Money>(m => m.Amount == 100.00m),
        ...),
    Times.Once);
```

#### 4. Verificação de Ordem de Chamadas
```csharp
// Usando callbacks para rastrear ordem
var callOrder = new List<string>();
_mockRepository
    .Setup(...)
    .Callback(() => callOrder.Add("GetProduct"));
```

#### 5. Verificação de Nenhuma Outra Chamada
```csharp
_mockPaymentGateway.VerifyNoOtherCalls();
```

#### 6. Simulação de Exceções
```csharp
_mockPaymentGateway
    .Setup(...)
    .ThrowsAsync(new Exception("Payment failed"));
```

## ⚡ Paralelismo de Testes

### Configuração Global (AssemblyInfo.cs)

```csharp
[assembly: CollectionBehavior(DisableTestParallelization = false, MaxParallelThreads = -1)]
```

- `DisableTestParallelization = false`: Permite paralelismo
- `MaxParallelThreads = -1`: Usa todos os cores disponíveis

### Collections Paralelas

**ParallelTestCollection.cs** e **ParallelTests.cs**

Testes que podem rodar em paralelo porque:
- Não compartilham estado mutável
- Não dependem de recursos externos compartilhados
- Cada teste é independente

```csharp
[Collection("Parallel Tests")]
public class ParallelTests
{
    [Fact]
    public void Test1_CanRunInParallel() { ... }
}
```

### Collections Sequenciais

**SequentialTestCollection.cs** e **SequentialTests.cs**

Testes que NÃO podem rodar em paralelo porque:
- Compartilham recursos (ex: banco de dados em memória)
- Podem causar condições de corrida
- Dependem de estado compartilhado

```csharp
[CollectionDefinition("Sequential Tests", DisableParallelization = true)]
public class SequentialTestCollection { ... }
```

**Quando usar collections sequenciais:**
- Testes que usam o mesmo banco de dados em memória
- Testes que escrevem no mesmo arquivo temporário
- Testes que modificam configurações globais
- Testes que dependem de ordem de execução

## 🚀 Executando os Testes

```bash
# Todos os testes unitários
dotnet test tests/MyShop.UnitTests/

# Apenas testes de services
dotnet test --filter "FullyQualifiedName~Service"

# Apenas testes de mocking
dotnet test --filter "FullyQualifiedName~Mock"

# Apenas testes paralelos
dotnet test --filter "Collection=Parallel Tests"

# Apenas testes sequenciais
dotnet test --filter "Collection=Sequential Tests"
```

## 📦 Pacotes NuGet

- `xunit` - Framework de testes
- `Moq` - Biblioteca de mocking
- `FluentAssertions` - Assertions mais legíveis
- `coverlet.msbuild` - Geração de cobertura

## 💡 Dicas

1. **Use mocks para isolar dependências:** Testes unitários devem testar apenas a lógica do service, não suas dependências.

2. **Teste casos de sucesso e falha:** Garanta que o código funciona corretamente e trata erros adequadamente.

3. **Use FluentAssertions:** Torna os testes mais legíveis e expressivos.

4. **Organize testes em collections:** Use collections para controlar quando testes podem rodar em paralelo.

5. **Siga o padrão AAA:** Arrange, Act, Assert torna os testes mais claros e fáceis de entender.

