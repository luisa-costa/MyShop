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

