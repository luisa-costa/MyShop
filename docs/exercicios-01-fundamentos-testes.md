# Exercícios - Módulo 1: Fundamentos de Testes no .NET

## Objetivos de Aprendizagem
- Compreender a importância dos testes automatizados
- Criar seus primeiros testes com xUnit
- Utilizar Assert para validações
- Gerar dados de teste com Bogus

---

## 1. Introdução aos Testes no .NET

### Por que testar?
- **Confiança**: Garante que o código funciona conforme esperado
- **Documentação**: Testes servem como documentação viva do comportamento do sistema
- **Refatoração segura**: Permite modificar código com segurança
- **Detecção precoce de bugs**: Encontra problemas antes de chegarem à produção

### Tipos de Testes
- **Testes Unitários**: Testam unidades isoladas de código (métodos, classes)
- **Testes de Integração**: Testam a integração entre componentes
- **Testes de API**: Testam endpoints HTTP

---

## 2. Configurando o Projeto de Testes

### Exercício 1.1: Analisar a estrutura do projeto de testes

**Tarefa**: Abra o arquivo [MyShop.UnitTests.csproj](../tests/MyShop.UnitTests/MyShop.UnitTests.csproj) e identifique:

1. Qual é o framework de testes utilizado?
2. Quais pacotes NuGet estão instalados?
3. O que cada pacote faz?

**Resultado esperado**: Você deve identificar:
- `xunit` - Framework de testes
- `xunit.runner.visualstudio` - Runner do xUnit para Visual Studio
- `Microsoft.NET.Test.Sdk` - SDK para execução de testes
- `Moq` - Biblioteca de mocking
- `coverlet.collector` e `coverlet.msbuild` - Ferramentas de cobertura de código

---

## 3. Criando o Primeiro Teste com xUnit

### Exercício 1.2: Criar teste simples para a classe Money

A classe `Money` é um Value Object que representa valores monetários no sistema.

**Tarefa**: Crie um novo arquivo `MoneyTests.cs` em `tests/MyShop.UnitTests/Domain/` com o seguinte teste:

```csharp
using MyShop.Domain;

namespace MyShop.UnitTests.Domain;

public class MoneyTests
{
    [Fact]
    public void Money_WithValidAmount_ShouldCreateInstance()
    {
        // Arrange
        var amount = 100.50m;
        var currency = "BRL";

        // Act
        var money = new Money(amount, currency);

        // Assert
        Assert.NotNull(money);
        Assert.Equal(amount, money.Amount);
        Assert.Equal(currency, money.Currency);
    }
}
```

**Conceitos importantes**:
- `[Fact]`: Atributo que marca um método como teste
- **AAA Pattern**: Arrange (preparar), Act (agir), Assert (verificar)
- `Assert`: Classe que contém métodos para validações

**Execute o teste**: Use o Test Explorer do Visual Studio ou execute via terminal:
```bash
dotnet test --filter "FullyQualifiedName~MoneyTests"
```

---

## 4. Assert e Validações

### Exercício 1.3: Testar validações da classe Money

**Tarefa**: Adicione os seguintes testes para validar regras de negócio:

```csharp
[Fact]
public void Money_WithNegativeAmount_ShouldThrowDomainException()
{
    // Arrange
    var negativeAmount = -50.00m;

    // Act & Assert
    var exception = Assert.Throws<DomainException>(() => new Money(negativeAmount));

    // Validação adicional da mensagem de erro
    Assert.Contains("Amount must be greater than or equal to zero", exception.Message);
}

[Fact]
public void Money_WithZeroAmount_ShouldCreateInstance()
{
    // Arrange & Act
    var money = new Money(0m);

    // Assert
    Assert.Equal(0m, money.Amount);
}
```

**Métodos de Assert comuns**:
- `Assert.Equal(expected, actual)` - Verifica igualdade
- `Assert.NotNull(object)` - Verifica que não é nulo
- `Assert.True(condition)` / `Assert.False(condition)` - Verifica booleanos
- `Assert.Throws<TException>()` - Verifica que uma exceção foi lançada
- `Assert.Contains(substring, string)` - Verifica se contém substring

---

## 5. Testes Parametrizados com Theory

### Exercício 1.4: Usar Theory para testar múltiplos valores

Quando precisamos testar o mesmo comportamento com diferentes valores, usamos `[Theory]` em vez de `[Fact]`.

**Tarefa**: Crie testes parametrizados para a classe Money:

```csharp
[Theory]
[InlineData(100.00, "BRL")]
[InlineData(50.50, "USD")]
[InlineData(0.01, "EUR")]
[InlineData(999999.99, "BRL")]
public void Money_WithValidData_ShouldCreateInstance(decimal amount, string currency)
{
    // Act
    var money = new Money(amount, currency);

    // Assert
    Assert.Equal(amount, money.Amount);
    Assert.Equal(currency, money.Currency);
}

[Theory]
[InlineData(-1)]
[InlineData(-100.50)]
[InlineData(-0.01)]
public void Money_WithNegativeAmount_ShouldThrowDomainException(decimal negativeAmount)
{
    // Act & Assert
    Assert.Throws<DomainException>(() => new Money(negativeAmount));
}
```

**Conceitos**:
- `[Theory]`: Marca um teste parametrizado
- `[InlineData(...)]`: Define um conjunto de parâmetros para o teste

---

## 6. Testando Coleções

### Exercício 1.5: Testar métodos que retornam coleções

Analise o teste em [ProductServiceTests.cs:99](../tests/MyShop.UnitTests/Services/ProductServiceTests.cs#L99) e crie um teste similar.

**Tarefa**: Observe como o teste `GetActiveProductsAsync_ShouldReturnOnlyActiveProducts` valida coleções:

```csharp
// Assert com coleções
Assert.Equal(2, result.Count());                    // Verifica quantidade
Assert.All(result, p => Assert.True(p.IsActive));   // Verifica condição em todos
Assert.Contains(activeProduct1, result);             // Verifica se contém item
Assert.DoesNotContain(inactiveProduct, result);     // Verifica que não contém item
```

**Métodos de Assert para coleções**:
- `Assert.Empty(collection)` - Verifica se está vazio
- `Assert.NotEmpty(collection)` - Verifica se não está vazio
- `Assert.Single(collection)` - Verifica se tem exatamente um elemento
- `Assert.Contains(item, collection)` - Verifica se contém item
- `Assert.DoesNotContain(item, collection)` - Verifica que não contém
- `Assert.All(collection, assertion)` - Aplica uma validação em todos os itens

---

## 7. Instalando e Utilizando o Bogus

### O que é Bogus?

Bogus é uma biblioteca para geração de dados falsos (fake data) para testes. É útil para criar dados de teste realistas e variados.

### Exercício 1.6: Instalar o Bogus

**Tarefa**: Adicione o pacote Bogus ao projeto de testes:

```bash
cd tests/MyShop.UnitTests
dotnet add package Bogus
```

### Exercício 1.7: Criar dados de teste com Bogus

**Tarefa**: Crie um arquivo `ProductTestsWithBogus.cs` e use Bogus para gerar produtos:

```csharp
using Bogus;
using MyShop.Domain;

namespace MyShop.UnitTests.Domain;

public class ProductTestsWithBogus
{
    private readonly Faker _faker;

    public ProductTestsWithBogus()
    {
        _faker = new Faker("pt_BR"); // Dados em português
    }

    [Fact]
    public void Product_CreatedWithBogus_ShouldHaveValidData()
    {
        // Arrange: Usa Bogus para gerar dados
        var name = _faker.Commerce.ProductName();
        var description = _faker.Commerce.ProductDescription();
        var price = new Money(_faker.Random.Decimal(1, 1000));
        var stock = _faker.Random.Int(0, 100);

        // Act
        var product = new Product(name, description, price, stock);

        // Assert
        Assert.NotNull(product);
        Assert.Equal(name, product.Name);
        Assert.Equal(description, product.Description);
        Assert.Equal(price, product.Price);
        Assert.Equal(stock, product.StockQuantity);
        Assert.True(product.IsActive);
    }

    [Fact]
    public void CreateMultipleProducts_WithBogus_ShouldGenerateDifferentData()
    {
        // Arrange & Act: Cria 10 produtos com dados aleatórios
        var products = new List<Product>();

        for (int i = 0; i < 10; i++)
        {
            var product = new Product(
                _faker.Commerce.ProductName(),
                _faker.Commerce.ProductDescription(),
                new Money(_faker.Random.Decimal(10, 500)),
                _faker.Random.Int(1, 50)
            );
            products.Add(product);
        }

        // Assert: Verifica que produtos diferentes foram criados
        Assert.Equal(10, products.Count);
        Assert.All(products, p => Assert.NotNull(p.Name));

        // Verifica que nem todos os nomes são iguais (dados variados)
        var uniqueNames = products.Select(p => p.Name).Distinct().Count();
        Assert.True(uniqueNames > 5); // Pelo menos 5 nomes diferentes
    }
}
```

### Exercício 1.8: Criar um Faker customizado para Product

**Tarefa**: Crie uma classe helper para gerar produtos de teste:

```csharp
public class ProductFaker
{
    private readonly Faker<Product> _faker;

    public ProductFaker()
    {
        _faker = new Faker<Product>("pt_BR")
            .CustomInstantiator(f => new Product(
                f.Commerce.ProductName(),
                f.Commerce.ProductDescription(),
                new Money(f.Random.Decimal(10, 1000)),
                f.Random.Int(0, 100)
            ));
    }

    public Product Generate()
    {
        return _faker.Generate();
    }

    public List<Product> Generate(int count)
    {
        return _faker.Generate(count);
    }
}
```

**Uso**:
```csharp
[Fact]
public void UsingProductFaker_ShouldSimplifyTestDataGeneration()
{
    // Arrange
    var productFaker = new ProductFaker();
    var products = productFaker.Generate(5);

    // Assert
    Assert.Equal(5, products.Count);
    Assert.All(products, p => Assert.True(p.StockQuantity >= 0));
}
```

---

## 8. Desafio Final do Módulo 1

### Exercício 1.9: Criar testes completos para a classe Product

**Tarefa**: Crie um arquivo `ProductTests.cs` completo que teste:

1. ✅ Criação de produto com dados válidos
2. ✅ Validação de nome vazio ou nulo (deve lançar `DomainException`)
3. ✅ Validação de preço zero ou negativo (deve lançar `DomainException`)
4. ✅ Validação de estoque negativo (deve lançar `DomainException`)
5. ✅ Método `UpdateStock` com valor válido
6. ✅ Método `UpdateStock` com valor negativo (deve lançar exceção)
7. ✅ Método `Deactivate` (produto deve ficar inativo)
8. ✅ Use Bogus para gerar pelo menos 3 testes com dados aleatórios

**Dicas**:
- Use `[Theory]` para testar múltiplos valores inválidos
- Use `Assert.Throws<DomainException>()` para validar exceções
- Consulte [ProductServiceTests.cs](../tests/MyShop.UnitTests/Services/ProductServiceTests.cs) como referência

---

## Checklist de Conclusão

Ao finalizar este módulo, você deve ser capaz de:

- [ ] Explicar a diferença entre `[Fact]` e `[Theory]`
- [ ] Usar o padrão AAA (Arrange, Act, Assert)
- [ ] Validar exceções com `Assert.Throws<T>()`
- [ ] Testar coleções com `Assert.Contains`, `Assert.All`, etc
- [ ] Instalar e usar o Bogus para gerar dados de teste
- [ ] Criar Fakers customizados para suas entidades
- [ ] Executar testes via Test Explorer e linha de comando

---

## Próximos Passos

No próximo módulo, você aprenderá sobre:
- Testando services (lógica de negócio)
- Mocking com Moq
- Paralelismo dos testes
- Debugar testes

Consulte: [exercicios-02-testes-unitarios-avancados.md](./exercicios-02-testes-unitarios-avancados.md)
