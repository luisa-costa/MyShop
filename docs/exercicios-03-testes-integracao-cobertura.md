# Exercícios - Módulo 3: Testes de Integração e Cobertura

## Objetivos de Aprendizagem
- Criar testes de integração com banco de dados em memória
- Testar APIs com WebApplicationFactory
- Usar Postman e Swagger para testes manuais
- Gerar e analisar relatórios de cobertura de código

---

## 1. Testes de Integração vs Testes Unitários

### Diferenças Fundamentais

| Aspecto | Testes Unitários | Testes de Integração |
|---------|------------------|----------------------|
| **Escopo** | Uma unidade isolada (classe/método) | Múltiplos componentes integrados |
| **Dependências** | Mocks/Stubs | Dependências reais (DB, APIs) |
| **Velocidade** | Muito rápidos (ms) | Mais lentos (segundos) |
| **Confiabilidade** | Detecta bugs lógicos | Detecta bugs de integração |
| **Isolamento** | Total | Parcial |

### Quando usar cada tipo?

**Testes Unitários**:
- Regras de negócio e validações
- Cálculos e transformações de dados
- Lógica condicional complexa

**Testes de Integração**:
- Persistência em banco de dados
- Chamadas entre camadas (API → Service → Repository → DB)
- Configuração de dependências (DI)

---

## 2. Configurando Testes de Integração com Banco em Memória

### Exercício 3.1: Entender o DatabaseFixture

**Tarefa**: Analise [DatabaseFixture.cs](../tests/MyShop.IntegrationTests/DatabaseFixture.cs) e responda:

1. Qual banco de dados é usado para testes?
2. Por que SQLite in-memory em vez de InMemory provider do EF Core?
3. Como é garantido o isolamento entre testes?

**Conceitos importantes**:
```csharp
// SQLite in-memory: banco real, mas na memória
var connection = new SqliteConnection("DataSource=:memory:");
connection.Open(); // Mantém conexão aberta

// Cada teste recebe um contexto isolado
public MyShopDbContext CreateContext()
{
    var context = new MyShopDbContext(_options);
    context.Database.EnsureCreated(); // Cria schema
    return context;
}
```

### Exercício 3.2: Criar seu primeiro teste de integração

**Tarefa**: Analise o teste em [EfCoreInMemoryIntegrationTests.cs:47](../tests/MyShop.IntegrationTests/EfCoreInMemoryIntegrationTests.cs#L47):

```csharp
[Fact]
public async Task CreateProduct_ShouldPersistToDatabase()
{
    // Arrange
    var name = "Test Product";
    var description = "Test Description";
    var price = new Money(99.99m);
    var stockQuantity = 10;

    // Act: Usa service REAL com repositório REAL e banco REAL (in-memory)
    var product = await _productService.CreateProductAsync(name, description, price, stockQuantity);

    // Assert: Verifica que foi salvo no banco
    var productFromDb = await _context.Products.FindAsync(product.Id);
    Assert.NotNull(productFromDb);
    Assert.Equal(name, productFromDb!.Name);
}
```

**Diferenças do teste unitário**:
- ❌ Sem mocks
- ✅ Service real
- ✅ Repositório real
- ✅ Banco de dados real (SQLite in-memory)

---

## 3. Testando Persistência e Queries

### Exercício 3.3: Testar queries complexas

**Tarefa**: Crie testes que validem queries do EF Core:

```csharp
[Fact]
public async Task GetActiveProducts_ShouldQueryDatabaseCorrectly()
{
    // Arrange: Popula o banco
    var activeProduct1 = new Product("Active 1", "Desc", new Money(10m), 5);
    var activeProduct2 = new Product("Active 2", "Desc", new Money(20m), 3);
    var inactiveProduct = new Product("Inactive", "Desc", new Money(30m), 0);
    inactiveProduct.Deactivate();

    _context.Products.AddRange(activeProduct1, activeProduct2, inactiveProduct);
    await _context.SaveChangesAsync();

    // Act: Executa query via service
    var result = await _productService.GetActiveProductsAsync();

    // Assert: Valida resultado da query
    Assert.Equal(2, result.Count());
    Assert.All(result, p => Assert.True(p.IsActive));

    // Valida que a query SQL foi eficiente (não trouxe produtos inativos)
    var allProductsInMemory = _context.Products.Local.Count;
    Assert.Equal(3, allProductsInMemory); // 3 foram criados mas apenas 2 retornados
}
```

### Exercício 3.4: Testar Value Objects com banco

**Tarefa**: Veja [EfCoreInMemoryIntegrationTests.cs:152](../tests/MyShop.IntegrationTests/EfCoreInMemoryIntegrationTests.cs#L152):

```csharp
[Fact]
public async Task ValueObjects_ShouldBePersistedCorrectly()
{
    // Arrange: Value Object complexo (Money)
    var product = new Product("Product", "Desc", new Money(123.45m, "BRL"), 10);
    _context.Products.Add(product);
    await _context.SaveChangesAsync();

    // Limpa o contexto (evita cache)
    _context.ChangeTracker.Clear();

    // Act: Busca do banco
    var productFromDb = await _context.Products.FindAsync(product.Id);

    // Assert: Value Object foi serializado/deserializado corretamente
    Assert.NotNull(productFromDb);
    Assert.Equal(123.45m, productFromDb!.Price.Amount);
    Assert.Equal("BRL", productFromDb.Price.Currency);
}
```

---

## 4. Testes de API com WebApplicationFactory

### Exercício 3.5: Entender WebApplicationFactory

**Tarefa**: Analise [WebApplicationFactory.cs](../tests/MyShop.ApiTests/WebApplicationFactory.cs):

**O que faz**:
1. Cria uma instância da API em memória (sem servidor HTTP real)
2. Substitui o banco de produção por SQLite in-memory
3. Permite fazer requests HTTP para a API

```csharp
public class MyShopWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove DbContext de produção
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<MyShopDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Adiciona DbContext de teste (SQLite in-memory)
            services.AddDbContext<MyShopDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });
        });
    }
}
```

### Exercício 3.6: Criar teste de API - GET

**Tarefa**: Crie um teste que chama um endpoint GET:

```csharp
using System.Net;
using System.Net.Http.Json;

public class ProductsControllerTests : IClassFixture<MyShopWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly MyShopDbContext _context;

    public ProductsControllerTests(MyShopWebApplicationFactory factory)
    {
        _client = factory.CreateClient();

        var scope = factory.Services.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
    }

    [Fact]
    public async Task GetProducts_ShouldReturnOk()
    {
        // Arrange: Cria produtos no banco
        var product1 = new Product("Product 1", "Desc", new Money(100m), 10);
        var product2 = new Product("Product 2", "Desc", new Money(200m), 5);
        _context.Products.AddRange(product1, product2);
        await _context.SaveChangesAsync();

        // Act: Faz request HTTP GET
        var response = await _client.GetAsync("/api/products");

        // Assert: Valida response
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        Assert.NotNull(products);
        Assert.Equal(2, products.Count);
    }

    [Fact]
    public async Task GetProductById_WhenExists_ShouldReturnProduct()
    {
        // Arrange
        var product = new Product("Test Product", "Desc", new Money(50m), 5);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/products/{product.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotNull(result);
        Assert.Equal("Test Product", result.Name);
    }

    [Fact]
    public async Task GetProductById_WhenNotExists_ShouldReturnNotFound()
    {
        // Act
        var nonExistentId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/products/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
```

### Exercício 3.7: Criar teste de API - POST

**Tarefa**: Analise o teste em [OrdersControllerTests.cs:32](../tests/MyShop.ApiTests/OrdersControllerTests.cs#L32):

```csharp
[Fact]
public async Task CreateOrder_WithValidData_ShouldReturnCreated()
{
    // Arrange: Prepara dados no banco
    var product1 = new Product("Product 1", "Description", new Money(100.00m), 10);
    var product2 = new Product("Product 2", "Description", new Money(50.00m), 5);
    _context.Products.AddRange(product1, product2);
    await _context.SaveChangesAsync();

    var createOrderDto = new
    {
        CustomerEmail = "customer@example.com",
        ShippingStreet = "123 Main St",
        ShippingCity = "São Paulo",
        ShippingState = "SP",
        ShippingZipCode = "01234-567",
        ShippingCountry = "Brasil",
        Items = new[]
        {
            new { ProductId = product1.Id, Quantity = 2 },
            new { ProductId = product2.Id, Quantity = 1 }
        }
    };

    // Act: POST request
    var response = await _client.PostAsJsonAsync("/api/orders", createOrderDto);

    // Assert
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    // Verifica side effects (estoque reduzido)
    await _context.Entry(product1).ReloadAsync();
    Assert.Equal(8, product1.StockQuantity); // 10 - 2
}
```

### Exercício 3.8: Testar validações da API

**Tarefa**: Crie testes que validam erros 400 (Bad Request):

```csharp
[Fact]
public async Task CreateProduct_WithInvalidData_ShouldReturnBadRequest()
{
    // Arrange: Dados inválidos (nome vazio)
    var invalidProduct = new
    {
        Name = "",
        Description = "Desc",
        Price = 100m,
        StockQuantity = 10
    };

    // Act
    var response = await _client.PostAsJsonAsync("/api/products", invalidProduct);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

    // Opcional: valida mensagem de erro
    var errorContent = await response.Content.ReadAsStringAsync();
    Assert.Contains("Name", errorContent);
}

[Theory]
[InlineData(-1)]    // Preço negativo
[InlineData(0)]     // Preço zero
public async Task CreateProduct_WithInvalidPrice_ShouldReturnBadRequest(decimal invalidPrice)
{
    // Arrange
    var product = new
    {
        Name = "Product",
        Description = "Desc",
        Price = invalidPrice,
        StockQuantity = 10
    };

    // Act
    var response = await _client.PostAsJsonAsync("/api/products", product);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}
```

---

## 5. Isolamento de Testes de API

### Exercício 3.9: Implementar IDisposable para limpeza

**Tarefa**: Veja [OrdersControllerTests.cs:136](../tests/MyShop.ApiTests/OrdersControllerTests.cs#L136):

```csharp
public class ProductsControllerTests : IClassFixture<MyShopWebApplicationFactory>, IDisposable
{
    private readonly MyShopWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly MyShopDbContext _context;

    public ProductsControllerTests(MyShopWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();

        var scope = _factory.Services.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
    }

    public void Dispose()
    {
        // Limpa o banco após cada teste
        _context.Products.RemoveRange(_context.Products);
        _context.SaveChanges();
        _context.Dispose();
        _client?.Dispose();
    }
}
```

**Por que é importante**:
- Garante que cada teste começa com banco limpo
- Evita interferência entre testes
- Previne vazamento de recursos

---

## 6. Testes de APIs com Postman e Swagger

### Exercício 3.10: Testar API manualmente com Swagger

**Tarefa**:
1. Execute a API: `dotnet run --project src/MyShop.API`
2. Abra o navegador: `https://localhost:5001/swagger`
3. Teste os seguintes endpoints:

**GET /api/products**
- Clique em "Try it out" → "Execute"
- Verifique o response (deve ser array vazio ou com produtos)

**POST /api/products**
- Clique em "Try it out"
- Preencha o JSON:
```json
{
  "name": "Notebook",
  "description": "Dell Inspiron 15",
  "price": 3500.00,
  "stockQuantity": 10
}
```
- Clique em "Execute"
- Verifique o status 201 Created

**GET /api/products/{id}**
- Use o ID retornado no POST anterior
- Verifique que o produto foi criado

### Exercício 3.11: Criar Collection no Postman

**Tarefa**: Veja a collection em [docs/postman/](../docs/postman/):

1. Abra o Postman
2. Importe a collection existente (se houver)
3. Crie as seguintes requests:

**Request 1: Create Product**
- Method: POST
- URL: `https://localhost:5001/api/products`
- Body (JSON):
```json
{
  "name": "{{$randomProductName}}",
  "description": "{{$randomProductAdjective}} product",
  "price": {{$randomPrice}},
  "stockQuantity": {{$randomInt}}
}
```
- Tests (JavaScript):
```javascript
pm.test("Status code is 201", function () {
    pm.response.to.have.status(201);
});

pm.test("Response has product ID", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.id).to.be.a('string');
    pm.environment.set("productId", jsonData.id);
});
```

**Request 2: Get Product by ID**
- Method: GET
- URL: `https://localhost:5001/api/products/{{productId}}`
- Tests:
```javascript
pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});

pm.test("Product name is not empty", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.name).to.not.be.empty;
});
```

**Request 3: Create Order**
- Method: POST
- URL: `https://localhost:5001/api/orders`
- Body:
```json
{
  "customerEmail": "{{$randomEmail}}",
  "shippingStreet": "{{$randomStreetAddress}}",
  "shippingCity": "São Paulo",
  "shippingState": "SP",
  "shippingZipCode": "01234-567",
  "items": [
    {
      "productId": "{{productId}}",
      "quantity": 2
    }
  ]
}
```

### Exercício 3.12: Executar testes automatizados no Postman

**Tarefa**:
1. Configure um Runner no Postman
2. Execute a collection completa
3. Verifique que todos os testes passam
4. Exporte os resultados

---

## 7. Geração de Relatório de Cobertura de Testes

### O que é Cobertura de Código?

**Code Coverage** mede qual porcentagem do código é executada pelos testes.

**Métricas**:
- **Line Coverage**: Linhas executadas
- **Branch Coverage**: Caminhos condicionais (if/else)
- **Method Coverage**: Métodos testados

### Exercício 3.13: Instalar Coverlet

Coverlet já está instalado no projeto. Verifique em [MyShop.UnitTests.csproj](../tests/MyShop.UnitTests/MyShop.UnitTests.csproj#L13):

```xml
<PackageReference Include="coverlet.collector" Version="6.0.0" />
<PackageReference Include="coverlet.msbuild" Version="6.0.0" />
```

### Exercício 3.14: Gerar relatório de cobertura

**Tarefa**: Execute os comandos para gerar cobertura:

```bash
# 1. Executar testes e coletar cobertura (formato padrão)
dotnet test /p:CollectCoverage=true

# 2. Gerar cobertura em formato Cobertura (XML)
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

# 3. Gerar cobertura com threshold mínimo (80%)
dotnet test /p:CollectCoverage=true /p:Threshold=80 /p:ThresholdType=line

# 4. Gerar cobertura apenas para o projeto MyShop.Application
dotnet test /p:CollectCoverage=true /p:Include="[MyShop.Application]*"

# 5. Excluir classes de testes da cobertura
dotnet test /p:CollectCoverage=true /p:Exclude="[*.Tests]*"
```

**Resultado**: Arquivo `coverage.cobertura.xml` será gerado.

### Exercício 3.15: Gerar relatório HTML com ReportGenerator

**Tarefa**: Instale o ReportGenerator e gere relatório visual:

```bash
# 1. Instalar ReportGenerator (global tool)
dotnet tool install -g dotnet-reportgenerator-globaltool

# 2. Executar testes e gerar cobertura
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=./TestResults/

# 3. Gerar relatório HTML
reportgenerator -reports:"./tests/**/TestResults/coverage.cobertura.xml" -targetdir:"./TestResults/CoverageReport" -reporttypes:Html

# 4. Abrir relatório no navegador
# Windows
start ./TestResults/CoverageReport/index.html

# Linux/Mac
open ./TestResults/CoverageReport/index.html
```

### Exercício 3.16: Analisar relatório de cobertura

**Tarefa**: Abra o relatório HTML gerado e analise:

1. **Summary Page**:
   - Line Coverage (%)
   - Branch Coverage (%)
   - Quais classes têm baixa cobertura?

2. **Class Details**:
   - Clique em uma classe (ex: ProductService)
   - Veja quais linhas estão cobertas (verde) e não cobertas (vermelho)
   - Identifique branches não testados

3. **Risk Hotspots**:
   - Métodos complexos com baixa cobertura
   - Priorize criar testes para esses métodos

### Exercício 3.17: Criar script para relatório automático

**Tarefa**: Crie um arquivo `coverage-report.bat` (Windows) ou `coverage-report.sh` (Linux/Mac):

**coverage-report.bat**:
```batch
@echo off
echo Executando testes e gerando cobertura...
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=./TestResults/

echo Gerando relatório HTML...
reportgenerator -reports:"./tests/**/TestResults/coverage.cobertura.xml" -targetdir:"./TestResults/CoverageReport" -reporttypes:Html

echo Abrindo relatório...
start ./TestResults/CoverageReport/index.html

echo Concluído!
```

**coverage-report.sh**:
```bash
#!/bin/bash
echo "Executando testes e gerando cobertura..."
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=./TestResults/

echo "Gerando relatório HTML..."
reportgenerator -reports:"./tests/**/TestResults/coverage.cobertura.xml" -targetdir:"./TestResults/CoverageReport" -reporttypes:Html

echo "Abrindo relatório..."
open ./TestResults/CoverageReport/index.html

echo "Concluído!"
```

Execute:
```bash
# Windows
coverage-report.bat

# Linux/Mac
chmod +x coverage-report.sh
./coverage-report.sh
```

---

## 8. CI/CD: Cobertura em Pipelines

### Exercício 3.18: Adicionar cobertura ao GitHub Actions

**Tarefa**: Crie `.github/workflows/tests.yml`:

```yaml
name: Tests with Coverage

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore

    - name: Test with Coverage
      run: dotnet test --no-build --verbosity normal /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

    - name: Generate Coverage Report
      run: |
        dotnet tool install -g dotnet-reportgenerator-globaltool
        reportgenerator -reports:"./tests/**/coverage.cobertura.xml" -targetdir:"./CoverageReport" -reporttypes:Html

    - name: Upload Coverage Report
      uses: actions/upload-artifact@v3
      with:
        name: coverage-report
        path: ./CoverageReport
```

---

## 9. Desafio Final do Módulo 3

### Exercício 3.19: Criar suíte completa de testes de integração

**Tarefa**: Crie testes que cubram todo o fluxo de um pedido:

1. **Teste de Integração com Banco**:
   ```csharp
   [Fact]
   public async Task CompleteOrderFlow_ShouldWorkEndToEnd()
   {
       // 1. Criar produtos
       // 2. Criar pedido
       // 3. Adicionar itens ao pedido
       // 4. Verificar que estoque foi reduzido
       // 5. Verificar que total foi calculado corretamente
   }
   ```

2. **Teste de API**:
   ```csharp
   [Fact]
   public async Task OrderApi_ShouldHandleCompleteFlow()
   {
       // 1. POST /api/products (criar 2 produtos)
       // 2. POST /api/orders (criar pedido com os produtos)
       // 3. GET /api/orders/{id} (verificar pedido criado)
       // 4. GET /api/products (verificar estoque reduzido)
   }
   ```

3. **Cobertura de Código**:
   - Execute: `dotnet test /p:CollectCoverage=true`
   - Gere relatório HTML
   - **Meta**: Atingir pelo menos 80% de cobertura em:
     - `ProductService`
     - `OrderService`
     - Controllers

4. **Collection do Postman**:
   - Crie uma collection com todos os endpoints
   - Adicione testes JavaScript
   - Execute a collection e exporte os resultados

---

## 10. Boas Práticas

### Checklist de Testes de Integração

- [ ] Cada teste é isolado (limpa dados após execução)
- [ ] Usa banco de dados de teste (não produção)
- [ ] Testa caminhos felizes e de erro
- [ ] Valida side effects (ex: estoque reduzido)
- [ ] Usa dados realistas (Bogus)
- [ ] Testes rodam em paralelo quando possível
- [ ] Usa `IClassFixture` para compartilhar setup
- [ ] Implementa `IDisposable` para cleanup

### Checklist de Cobertura

- [ ] Cobertura de linhas > 80%
- [ ] Cobertura de branches > 70%
- [ ] Métodos críticos têm 100% cobertura
- [ ] Relatório gerado automaticamente
- [ ] CI/CD valida cobertura mínima
- [ ] Hotspots de risco são priorizados

---

## Checklist de Conclusão

Ao finalizar este módulo, você deve ser capaz de:

- [ ] Diferenciar testes unitários de testes de integração
- [ ] Configurar banco de dados em memória (SQLite)
- [ ] Criar testes de integração com EF Core
- [ ] Testar APIs com WebApplicationFactory
- [ ] Fazer requests HTTP em testes (GET, POST, PUT, DELETE)
- [ ] Usar Postman para testes manuais e automatizados
- [ ] Gerar relatório de cobertura com Coverlet
- [ ] Visualizar cobertura com ReportGenerator
- [ ] Integrar cobertura em CI/CD

---

## Recursos Adicionais

### Documentação
- [xUnit Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4)
- [Coverlet Documentation](https://github.com/coverlet-coverage/coverlet)
- [WebApplicationFactory](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)

### Ferramentas
- [ReportGenerator](https://github.com/danielpalme/ReportGenerator)
- [Postman](https://www.postman.com/)
- [Swagger](https://swagger.io/)

### Artigos
- [Testing Best Practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
- [Integration Testing in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)

---

## Parabéns! 🎉

Você completou todos os 3 módulos de testes do MyShop!

**O que você aprendeu**:
- ✅ Fundamentos de testes com xUnit
- ✅ Geração de dados com Bogus
- ✅ Mocking com Moq
- ✅ Testes de integração com banco em memória
- ✅ Testes de APIs
- ✅ Cobertura de código

**Próximos passos**:
- Pratique criando testes para seus próprios projetos
- Explore testes de performance com BenchmarkDotNet
- Aprenda sobre testes E2E com Selenium ou Playwright
- Contribua com projetos open-source escrevendo testes
