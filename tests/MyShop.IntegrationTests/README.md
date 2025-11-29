# MyShop.IntegrationTests

Este projeto contém testes de integração usando Entity Framework Core com banco de dados em memória (SQLite).

## 📁 Estrutura

```
MyShop.IntegrationTests/
├── DatabaseFixture.cs              # Fixture para configuração do banco
└── EfCoreInMemoryIntegrationTests.cs  # Testes de integração com EF Core
```

## 🎯 Objetivo

Testes de integração validam que diferentes componentes do sistema funcionam corretamente juntos. Ao contrário dos testes unitários (que usam mocks), estes testes usam implementações reais.

## 🗄️ Configuração do Banco de Dados

### DatabaseFixture.cs

A fixture configura SQLite in-memory para os testes:

```csharp
public MyShopDbContext CreateContext()
{
    var connection = new SqliteConnection("DataSource=:memory:");
    connection.Open();
    
    var options = new DbContextOptionsBuilder<MyShopDbContext>()
        .UseSqlite(connection)
        .Options;
    
    var context = new MyShopDbContext(options);
    context.Database.EnsureCreated(); // Cria o schema
    
    return context;
}
```

**Por que SQLite in-memory ao invés de InMemory provider?**
- SQLite suporta migrations (mais próximo do banco real)
- Comportamento mais próximo de um banco relacional real
- Suporta transações e constraints

## 🧪 Testes de Integração

### EfCoreInMemoryIntegrationTests.cs

Demonstra como testar interações reais com o banco de dados.

**Características:**
- Cada teste recebe um banco de dados isolado
- Usa repositórios reais (não mocks)
- Valida persistência e recuperação de dados
- Testa Value Objects e relacionamentos

**Exemplo:**
```csharp
[Fact]
public async Task CreateProduct_ShouldPersistToDatabase()
{
    // Arrange
    var product = await _productService.CreateProductAsync(...);
    
    // Act: Busca diretamente do banco
    var productFromDb = await _context.Products.FindAsync(product.Id);
    
    // Assert: Verifica que foi persistido
    productFromDb.Should().NotBeNull();
    productFromDb.Name.Should().Be("Test Product");
}
```

## 🔒 Isolamento entre Testes

Cada teste recebe seu próprio banco de dados:

```csharp
public EfCoreInMemoryIntegrationTests(DatabaseFixture fixture)
{
    // Cada teste cria seu próprio contexto
    _context = _fixture.CreateContext();
}
```

Isso garante que:
- Testes não interferem uns nos outros
- Cada teste começa com um banco limpo
- Testes podem rodar em qualquer ordem

## ⚠️ Limitações dos Providers em Memória

### SQLite in-memory vs Banco Real

**Limitações:**
- Não suporta todas as funcionalidades de SQL Server
- Alguns tipos de dados podem se comportar diferentemente
- Constraints complexas podem não ser validadas da mesma forma
- Performance pode ser diferente

**Quando usar:**
- ✅ Testes de integração rápidos
- ✅ Validação de lógica de aplicação
- ✅ Testes de repositórios

**Quando NÃO usar:**
- ❌ Testes que dependem de funcionalidades específicas do SQL Server
- ❌ Testes de performance
- ❌ Testes que precisam validar migrations complexas

**Alternativas para testes mais próximos da produção:**
- **Testcontainers:** Docker containers com banco real
- **Banco de dados de teste dedicado**
- **Testes em ambiente de staging**

## 🚀 Executando os Testes

```bash
# Todos os testes de integração
dotnet test tests/MyShop.IntegrationTests/

# Com logging detalhado
dotnet test tests/MyShop.IntegrationTests/ --logger "console;verbosity=detailed"
```

## 📦 Pacotes NuGet

- `Microsoft.EntityFrameworkCore.Sqlite` - Provider SQLite
- `FluentAssertions` - Assertions legíveis
- `coverlet.msbuild` - Cobertura de testes

## 💡 Diferença entre Testes Unitários e de Integração

| Aspecto | Testes Unitários | Testes de Integração |
|---------|------------------|----------------------|
| **Velocidade** | Muito rápidos | Mais lentos |
| **Isolamento** | Usam mocks | Usam implementações reais |
| **Escopo** | Testam uma unidade | Testam integração entre componentes |
| **Banco de dados** | Não usam | Usam banco em memória |
| **Quando usar** | Lógica de negócio | Integração entre camadas |

## 🎓 Boas Práticas

1. **Isolamento:** Cada teste deve ter seu próprio banco de dados
2. **Limpeza:** Sempre limpe o banco após cada teste
3. **Velocidade:** Mantenha testes de integração rápidos (use in-memory)
4. **Foco:** Teste integração, não lógica de negócio (isso é para testes unitários)
5. **Organização:** Use collections para agrupar testes relacionados

