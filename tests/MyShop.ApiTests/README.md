# MyShop.ApiTests

Este projeto contém testes de integração para a API REST, usando `WebApplicationFactory` para criar uma instância da aplicação em memória.

## 📁 Estrutura

```
MyShop.ApiTests/
├── WebApplicationFactory.cs      # Factory para criar aplicação de teste
├── ProductsControllerTests.cs    # Testes dos endpoints de produtos
└── OrdersControllerTests.cs      # Testes dos endpoints de pedidos
```

## 🎯 Objetivo

Testes de API validam que os endpoints HTTP funcionam corretamente, incluindo:
- Status codes corretos
- Respostas no formato esperado
- Integração entre controllers, services e banco de dados

## 🏭 WebApplicationFactory

### O que é?

`WebApplicationFactory` permite criar uma instância da aplicação ASP.NET Core em memória para testes, sem precisar iniciar um servidor HTTP real.

**Vantagens:**
- ✅ Mais rápido que iniciar um servidor real
- ✅ Pode ser executado em CI/CD
- ✅ Isolamento completo entre testes
- ✅ Configuração customizada para testes

### Configuração

```csharp
public class MyShopWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Substitui serviços para testes
        builder.ConfigureServices(services =>
        {
            // Remove DbContext de produção
            // Adiciona SQLite in-memory
            services.AddDbContext<MyShopDbContext>(options =>
            {
                options.UseSqlite("DataSource=:memory:");
            });
        });
    }
}
```

## 🔧 Correção Crítica: SQLite In-Memory e Fixtures

### ⚠️ O Problema

Inicialmente, os testes falhavam com o erro:
```
SQLite Error 1: 'no such table: Products'
```

### 🔍 Por Que Aconteceu?

O problema ocorreu porque **SQLite in-memory tem um comportamento especial**:

1. **Conexão temporária**: Quando usamos `"DataSource=:memory:"` diretamente, cada vez que uma conexão é criada, um novo banco em memória é criado
2. **Conexão fechada = banco perdido**: Quando a conexão é fechada, o banco em memória é **completamente descartado**
3. **Múltiplas conexões = múltiplos bancos**: Cada `DbContext` criava sua própria conexão, resultando em bancos diferentes

**Código problemático:**
```csharp
// ❌ ERRADO: Cada DbContext cria sua própria conexão
services.AddDbContext<MyShopDbContext>(options =>
{
    options.UseSqlite("DataSource=:memory:"); // Nova conexão a cada vez!
});
```

### ✅ A Solução

A solução envolve **manter uma conexão SQLite compartilhada** que permanece aberta durante toda a vida útil da `WebApplicationFactory`:

```csharp
public class MyShopWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection; // Conexão compartilhada
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // ✅ CORRETO: Cria uma conexão compartilhada
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open(); // Mantém aberta!
            
            services.AddDbContext<MyShopDbContext>(options =>
            {
                options.UseSqlite(_connection); // Usa a mesma conexão
            });
        });
    }
    
    protected override void Dispose(bool disposing)
    {
        _connection?.Dispose(); // Fecha apenas quando a factory é descartada
        base.Dispose(disposing);
    }
}
```

### 🎯 Por Que Funciona?

1. **Conexão única**: Uma única conexão SQLite é criada e mantida aberta
2. **Banco persistente**: Como a conexão nunca fecha, o banco permanece em memória
3. **Compartilhamento**: Todos os `DbContext` da mesma factory usam o mesmo banco
4. **Isolamento**: Cada `WebApplicationFactory` tem seu próprio banco isolado

## 📚 A Importância das Fixtures (IClassFixture)

### O Que São Fixtures?

**Fixtures** no xUnit são classes que permitem **compartilhar setup e teardown** entre múltiplos testes. Elas são especialmente úteis para recursos caros de configurar, como:

- Conexões de banco de dados
- Servidores HTTP
- Configurações complexas
- Recursos externos

### Como Funciona IClassFixture?

```csharp
// 1. Define a fixture
public class MyShopWebApplicationFactory : WebApplicationFactory<Program> { }

// 2. Usa a fixture na classe de testes
public class ProductsControllerTests : IClassFixture<MyShopWebApplicationFactory>
{
    private readonly MyShopWebApplicationFactory _factory;
    
    public ProductsControllerTests(MyShopWebApplicationFactory factory)
    {
        _factory = factory; // Recebe a mesma instância em todos os testes
        _client = _factory.CreateClient();
    }
}
```

### 🔄 Ciclo de Vida das Fixtures

```
┌─────────────────────────────────────────────────────────┐
│ 1. xUnit cria UMA instância de MyShopWebApplicationFactory │
└─────────────────────────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────┐
│ 2. ConfigureWebHost() é chamado UMA vez                  │
│    - Cria conexão SQLite compartilhada                  │
│    - Configura serviços                                  │
└─────────────────────────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────┐
│ 3. Construtor de ProductsControllerTests é chamado      │
│    - Para CADA teste (mas recebe a MESMA factory)       │
│    - CreateClient() cria o banco se necessário          │
└─────────────────────────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────┐
│ 4. Testes executam (compartilhando a mesma factory)     │
│    - Todos usam o mesmo banco SQLite                    │
│    - Cada teste limpa seus dados no Dispose()           │
└─────────────────────────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────┐
│ 5. Quando TODOS os testes terminam                      │
│    - Dispose() da factory é chamado                          │
│    - Conexão SQLite é fechada                           │
│    - Banco em memória é descartado                      │
└─────────────────────────────────────────────────────────┘
```

### 💡 Benefícios das Fixtures

#### 1. **Performance**
- ✅ Setup caro é feito **uma única vez**
- ✅ Recursos são compartilhados entre testes
- ✅ Testes executam mais rápido

**Sem fixture (lento):**
```csharp
// ❌ Cada teste cria sua própria factory
[Fact]
public async Task Test1()
{
    var factory = new WebApplicationFactory<Program>(); // Setup caro!
    var client = factory.CreateClient();
    // ...
}

[Fact]
public async Task Test2()
{
    var factory = new WebApplicationFactory<Program>(); // Setup caro novamente!
    var client = factory.CreateClient();
    // ...
}
```

**Com fixture (rápido):**
```csharp
// ✅ Factory é criada uma vez, compartilhada por todos
public class Tests : IClassFixture<MyShopWebApplicationFactory>
{
    private readonly MyShopWebApplicationFactory _factory;
    
    public Tests(MyShopWebApplicationFactory factory)
    {
        _factory = factory; // Recebe a mesma instância
    }
    
    [Fact] public async Task Test1() { /* usa _factory */ }
    [Fact] public async Task Test2() { /* usa _factory */ }
}
```

#### 2. **Compartilhamento de Contexto**
- ✅ Todos os testes da mesma classe compartilham a mesma factory
- ✅ Banco de dados, serviços e configurações são compartilhados
- ✅ Permite testes que dependem de estado compartilhado

#### 3. **Isolamento entre Classes**
- ✅ Cada classe de teste recebe sua **própria instância** da fixture
- ✅ Testes de classes diferentes não interferem entre si
- ✅ Cada classe tem seu próprio banco de dados isolado

### ⚠️ Cuidados Importantes

#### 1. **Limpeza de Dados**

Como os testes compartilham o mesmo banco, **é essencial limpar os dados** após cada teste:

```csharp
public void Dispose()
{
    // ✅ SEMPRE limpe os dados após cada teste
    _context.Products.RemoveRange(_context.Products);
    _context.Orders.RemoveRange(_context.Orders);
    _context.SaveChanges();
    _context.Dispose();
}
```

**Sem limpeza:**
```csharp
// ❌ Teste 1 cria um produto
[Fact] public async Task Test1() 
{ 
    _context.Products.Add(new Product(...));
    await _context.SaveChangesAsync();
}

// ❌ Teste 2 pode encontrar o produto do Teste 1!
[Fact] public async Task Test2() 
{ 
    var products = await _context.Products.ToListAsync();
    // products contém dados do Teste 1! 💥
}
```

#### 2. **Thread Safety**

Quando o banco é criado, usamos `lock` para garantir thread-safety:

```csharp
private static bool _databaseInitialized = false;
private static readonly object _lock = new object();

public override HttpClient CreateClient()
{
    lock (_lock) // ✅ Protege contra race conditions
    {
        if (!_databaseInitialized)
        {
            // Cria banco apenas uma vez
            context.Database.EnsureCreated();
            _databaseInitialized = true;
        }
    }
}
```

#### 3. **Ordem de Execução**

- ⚠️ **Não assuma ordem**: Testes podem executar em qualquer ordem
- ✅ **Sempre limpe**: Cada teste deve começar com um banco limpo
- ✅ **Não dependa de outros**: Testes devem ser independentes

### 📊 Comparação: Com vs Sem Fixture

| Aspecto | Sem Fixture | Com Fixture (IClassFixture) |
|---------|-------------|------------------------------|
| **Setup** | A cada teste | Uma vez por classe |
| **Performance** | Lento (setup repetido) | Rápido (setup compartilhado) |
| **Isolamento** | Total (cada teste isolado) | Entre classes (testes da mesma classe compartilham) |
| **Uso de memória** | Alto (múltiplas factories) | Baixo (uma factory por classe) |
| **Complexidade** | Simples | Requer limpeza cuidadosa |

### 🎓 Quando Usar Fixtures?

**Use `IClassFixture` quando:**
- ✅ Setup é caro (criar aplicação, banco de dados, etc.)
- ✅ Recursos podem ser compartilhados com segurança
- ✅ Você pode garantir limpeza adequada entre testes
- ✅ Performance é importante

**NÃO use `IClassFixture` quando:**
- ❌ Testes precisam de isolamento total
- ❌ Setup é rápido e barato
- ❌ Limpeza é complexa ou impossível
- ❌ Testes dependem de estado específico que não pode ser compartilhado

### 🔗 Outros Tipos de Fixtures no xUnit

1. **`IClassFixture<T>`**: Compartilha entre testes da mesma classe
2. **`ICollectionFixture<T>`**: Compartilha entre múltiplas classes (usando `[Collection]`)
3. **`IAsyncLifetime`**: Para setup/teardown assíncrono

**Exemplo de CollectionFixture:**
```csharp
[CollectionDefinition("Database Tests")]
public class DatabaseTestCollection : ICollectionFixture<DatabaseFixture> { }

[Collection("Database Tests")]
public class TestClass1 : IClassFixture<MyShopWebApplicationFactory> { }

[Collection("Database Tests")]
public class TestClass2 : IClassFixture<MyShopWebApplicationFactory> { }
```

## 🧪 Testes de API

### ProductsControllerTests.cs

Demonstra como testar endpoints HTTP:

```csharp
[Fact]
public async Task GetProducts_ShouldReturnOk()
{
    // Arrange: Prepara dados no banco
    _context.Products.AddRange(...);
    await _context.SaveChangesAsync();

    // Act: Faz requisição HTTP
    var response = await _client.GetAsync("/api/products");

    // Assert: Verifica resposta
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var products = await response.Content.ReadFromJsonAsync<List<Product>>();
    products.Should().NotBeNull();
}
```

**Conceitos demonstrados:**
- Requisições HTTP reais (GET, POST, PUT)
- Verificação de status codes
- Deserialização de respostas JSON
- Validação de dados persistidos

### OrdersControllerTests.cs

Testa endpoints mais complexos que envolvem múltiplas entidades e regras de negócio.

## 🔄 Testes Automatizados vs Postman

### Testes Automatizados em C# (Este Projeto)

**Vantagens:**
- ✅ Executáveis em CI/CD
- ✅ Mais rápidos (não precisam de interface)
- ✅ Garantem que API funciona após mudanças
- ✅ Podem ser executados em paralelo
- ✅ Integrados ao processo de desenvolvimento

**Desvantagens:**
- ❌ Requer conhecimento de C#
- ❌ Menos visual/interativo
- ❌ Mais difícil de compartilhar com não-desenvolvedores

### Testes no Postman

**Vantagens:**
- ✅ Interface visual amigável
- ✅ Fácil de compartilhar (collections)
- ✅ Permite testes exploratórios
- ✅ Útil para documentação
- ✅ Acessível para não-desenvolvedores

**Desvantagens:**
- ❌ Requer execução manual
- ❌ Não integrado ao CI/CD (sem scripts)
- ❌ Mais lento para grandes suites de testes

### Quando Usar Cada Um?

**Use testes automatizados em C# quando:**
- Quiser garantir que API funciona após mudanças
- Precisar executar em CI/CD
- Quiser feedback rápido durante desenvolvimento
- Precisar testar muitos cenários rapidamente

**Use Postman quando:**
- Quiser explorar a API manualmente
- Precisar compartilhar exemplos com equipe
- Quiser documentar a API visualmente
- Precisar testar cenários específicos rapidamente

**Ideal:** Use ambos! Testes automatizados para garantir qualidade e Postman para exploração e documentação.

## 🚀 Executando os Testes

```bash
# Todos os testes de API
dotnet test tests/MyShop.ApiTests/

# Apenas testes de produtos
dotnet test --filter "FullyQualifiedName~Product"

# Apenas testes de pedidos
dotnet test --filter "FullyQualifiedName~Order"
```

## 📦 Pacotes NuGet

- `Microsoft.AspNetCore.Mvc.Testing` - WebApplicationFactory
- `Microsoft.EntityFrameworkCore.Sqlite` - Provider SQLite para testes
- `FluentAssertions` - Assertions legíveis
- `coverlet.msbuild` - Cobertura de testes

## 💡 Boas Práticas

1. **Isolamento:** Cada teste deve ter seu próprio banco de dados limpo
2. **Limpeza:** Sempre limpe o banco após cada teste (use `Dispose()`)
3. **Assertions claras:** Use FluentAssertions para assertions mais legíveis
4. **Cenários realistas:** Teste casos de sucesso e falha
5. **Status codes:** Sempre verifique os status codes corretos

## 🔗 Postman Collection

Uma collection do Postman está disponível em `docs/postman/MyShop.postman_collection.json`.

Para usar:
1. Abra o Postman
2. Importe a collection
3. Configure a variável `baseUrl` para `https://localhost:5001` (ou a URL da sua API)
4. Execute as requisições

