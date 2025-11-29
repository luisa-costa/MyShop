# MyShop - Sistema de Testes Didático para C#/.NET

Este repositório é um exemplo didático completo de testes automatizados em C#/.NET, cobrindo desde testes unitários até testes de integração e API.

## 📚 Visão Geral

Este projeto demonstra as melhores práticas de testes em .NET, incluindo:

1. **Testes de Services (Lógica de Negócio)** - Testes unitários focados em regras de negócio
2. **Mocking com Moq** - Como mockar dependências e verificar interações
3. **Paralelismo de Testes** - Como configurar e gerenciar execução paralela de testes
4. **Execução via Linha de Comando** - Comandos para executar testes em diferentes cenários
5. **Testes de Integração com EF Core** - Testes com banco de dados em memória
6. **Testes de API** - Testes automatizados de endpoints HTTP
7. **Cobertura de Testes** - Como gerar e analisar relatórios de cobertura

## 🏗️ Estrutura do Projeto

```
MyShop/
├── src/
│   ├── MyShop.API/              # API REST com ASP.NET Core
│   ├── MyShop.Application/       # Camada de aplicação (Services, Interfaces, DTOs)
│   ├── MyShop.Domain/            # Camada de domínio (Entidades, Value Objects)
│   └── MyShop.Infrastructure/    # Camada de infraestrutura (Repositories, DbContext)
└── tests/
    ├── MyShop.UnitTests/         # Testes unitários (Services, Mocking, Collections)
    ├── MyShop.IntegrationTests/  # Testes de integração (EF Core in-memory)
    └── MyShop.ApiTests/          # Testes de API (WebApplicationFactory)
```

## 📖 Onde Encontrar Cada Tópico

### 1. Testes de Services (Lógica de Negócio)
**Localização:** `tests/MyShop.UnitTests/Services/`
- `ProductServiceTests.cs` - Testes de lógica de negócio com padrão AAA
- `OrderServiceTests.cs` - Testes de regras complexas (frete, descontos)

**Conceitos demonstrados:**
- Padrão AAA (Arrange, Act, Assert)
- Testes de casos de sucesso e falha
- Validações de regras de negócio

### 2. Mocking com Moq
**Localização:** `tests/MyShop.UnitTests/Mocking/`
- `OrderServiceWithMocksTests.cs` - Exemplos avançados de Moq

**Conceitos demonstrados:**
- Setup de métodos (Returns, Throws)
- Verificação de chamadas (Verify, VerifyNoOtherCalls)
- Uso de `It.IsAny<T>()` e `It.Is<T>(predicate)`
- Callbacks para capturar parâmetros

### 3. Paralelismo de Testes
**Localização:** `tests/MyShop.UnitTests/Collections/`
- `ParallelTestCollection.cs` - Collection que permite paralelismo
- `SequentialTestCollection.cs` - Collection que desabilita paralelismo
- `AssemblyInfo.cs` - Configuração global de paralelismo

**Conceitos demonstrados:**
- Quando é seguro rodar testes em paralelo
- Quando NÃO é seguro e por quê
- Como usar `[Collection]` e `[CollectionDefinition]`

### 4. Execução via Linha de Comando
Veja a seção [Executando Testes via Linha de Comando](#executando-testes-via-linha-de-comando) abaixo.

### 5. Testes de Integração com EF Core
**Localização:** `tests/MyShop.IntegrationTests/`
- `EfCoreInMemoryIntegrationTests.cs` - Testes com SQLite in-memory
- `DatabaseFixture.cs` - Fixture para configuração do banco

**Conceitos demonstrados:**
- Configuração de SQLite in-memory
- Aplicação de migrations
- Isolamento entre testes
- Limitações dos providers em memória

### 6. Testes de API
**Localização:** `tests/MyShop.ApiTests/`
- `ProductsControllerTests.cs` - Testes de endpoints de produtos
- `OrdersControllerTests.cs` - Testes de endpoints de pedidos
- `WebApplicationFactory.cs` - Factory para criar aplicação de teste

**Conceitos demonstrados:**
- Uso de `WebApplicationFactory`
- Testes de endpoints HTTP
- Diferença entre testes automatizados vs Postman

**Postman Collection:** `docs/postman/MyShop.postman_collection.json`

### 7. Cobertura de Testes
Veja a seção [Cobertura de Testes](#cobertura-de-testes) abaixo.

## 🚀 Como Começar

### Pré-requisitos
- .NET 8.0 SDK
- Visual Studio 2022, VS Code ou Rider (opcional)

### Instalação

1. Clone o repositório:
```bash
git clone <repository-url>
cd MyShop
```

2. Restaure as dependências:
```bash
dotnet restore
```

3. Execute os testes:
```bash
dotnet test
```

## 🧪 Executando Testes via Linha de Comando

### Executar Todos os Testes
```bash
# Executa todos os testes de todos os projetos
dotnet test
```

### Executar Testes de um Projeto Específico
```bash
# Apenas testes unitários
dotnet test tests/MyShop.UnitTests/MyShop.UnitTests.csproj

# Apenas testes de integração
dotnet test tests/MyShop.IntegrationTests/MyShop.IntegrationTests.csproj

# Apenas testes de API
dotnet test tests/MyShop.ApiTests/MyShop.ApiTests.csproj
```

### Filtrar Testes por Nome
```bash
# Executa apenas testes que contenham "Product" no nome
dotnet test --filter "FullyQualifiedName~Product"

# Executa apenas testes que contenham "Order" no nome
dotnet test --filter "FullyQualifiedName~Order"
```

### Filtrar Testes por Categoria/Trait
```bash
# Executa apenas testes marcados com [Fact] (sem traits específicos)
dotnet test --filter "Category=Unit"

# Executa testes de uma collection específica
dotnet test --filter "Collection=Parallel Tests"
```

### Executar com Logging Detalhado
```bash
# Logging normal
dotnet test --logger "console;verbosity=normal"

# Logging detalhado
dotnet test --logger "console;verbosity=detailed"

# Logging mínimo
dotnet test --logger "console;verbosity=minimal"
```

### Executar com Resultados em Arquivo
```bash
# Salva resultados em formato TRX (para Visual Studio)
dotnet test --logger "trx;LogFileName=test-results.trx"

# Salva resultados em formato JUnit (para CI/CD)
dotnet test --logger "junit;LogFileName=test-results.xml"
```

### Executar Testes em Paralelo
```bash
# Por padrão, o xUnit executa testes em paralelo
# Para desabilitar paralelismo:
dotnet test -- --no-parallel

# Para limitar o número de threads:
dotnet test -- --maxparallel 2
```

### Executar Testes com Configuração Específica
```bash
# Executa apenas testes de Debug
dotnet test --configuration Debug

# Executa apenas testes de Release
dotnet test --configuration Release
```

## 📊 Cobertura de Testes

### O que é Cobertura de Testes?

Cobertura de testes é uma métrica que indica qual porcentagem do código foi executada pelos testes. É uma ferramenta útil para identificar áreas do código que não estão sendo testadas.

**⚠️ IMPORTANTE:** Cobertura alta não garante qualidade! Código pode ter 100% de cobertura mas ainda ter bugs se os testes não verificarem os comportamentos corretos.

### Gerando Relatório de Cobertura

Este projeto usa **coverlet** para gerar relatórios de cobertura.

#### Instalação (já configurado)
O pacote `coverlet.msbuild` já está instalado em todos os projetos de teste.

#### Comandos para Gerar Cobertura

```bash
# Gera cobertura em formato XML (cobertura padrão)
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Gera cobertura em formato JSON
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=json

# Gera cobertura em formato Cobertura (para Azure DevOps)
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

# Gera cobertura e salva em arquivo específico
dotnet test /p:CollectCoverage=true /p:CoverletOutput=./coverage/ /p:CoverletOutputFormat=opencover
```

#### Gerar Relatório HTML (Recomendado)

Para visualizar a cobertura de forma mais amigável, use o **ReportGenerator**:

1. Instale o ReportGenerator globalmente:
```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

2. Gere a cobertura e o relatório HTML:
```bash
# Gera cobertura
dotnet test /p:CollectCoverage=true /p:CoverletOutput=./coverage/ /p:CoverletOutputFormat=opencover

# Gera relatório HTML
reportgenerator -reports:"./coverage/coverage.opencover.xml" -targetdir:"./coverage/html" -reporttypes:Html
```

3. Abra o arquivo `./coverage/html/index.html` no navegador.

#### Excluir Arquivos da Cobertura

Para excluir arquivos específicos da cobertura, adicione no `.csproj`:

```xml
<ItemGroup>
  <ExcludeFromCoverage Include="**/Migrations/**" />
  <ExcludeFromCoverage Include="**/Program.cs" />
</ItemGroup>
```

### Limitações da Cobertura

- **Cobertura não garante qualidade:** Código pode ter 100% de cobertura mas ainda ter bugs
- **Cobertura não testa lógica:** Apenas indica se o código foi executado, não se o comportamento está correto
- **Falsos positivos:** Código pode estar "coberto" mas não testado adequadamente
- **Foco em quantidade vs qualidade:** É melhor ter menos testes bem escritos do que muitos testes ruins

## 📝 Passo a Passo Sugerido para Estudo

### 1. Comece pelos Fundamentos
1. Leia `tests/MyShop.UnitTests/Services/ProductServiceTests.cs`
   - Entenda o padrão AAA
   - Veja como testar casos de sucesso e falha

### 2. Entenda Mocking
1. Leia `tests/MyShop.UnitTests/Mocking/OrderServiceWithMocksTests.cs`
   - Veja como mockar dependências
   - Entenda Verify, It.IsAny, etc.

### 3. Explore Paralelismo
1. Leia `tests/MyShop.UnitTests/Collections/`
   - Entenda quando usar paralelismo
   - Veja a diferença entre collections paralelas e sequenciais

### 4. Testes de Integração
1. Leia `tests/MyShop.IntegrationTests/EfCoreInMemoryIntegrationTests.cs`
   - Veja como testar com banco de dados
   - Entenda isolamento entre testes

### 5. Testes de API
1. Leia `tests/MyShop.ApiTests/ProductsControllerTests.cs`
   - Veja como testar endpoints HTTP
   - Compare com testes manuais no Postman

### 6. Prática
1. Execute os testes: `dotnet test`
2. Gere cobertura: `dotnet test /p:CollectCoverage=true`
3. Experimente modificar o código e veja os testes falharem
4. Adicione novos testes para praticar

## 🔧 Pacotes NuGet Utilizados

### Testes Unitários
- `xunit` - Framework de testes
- `Moq` - Biblioteca de mocking
- `FluentAssertions` - Assertions mais legíveis
- `coverlet.msbuild` - Geração de cobertura

### Testes de Integração
- `Microsoft.EntityFrameworkCore.Sqlite` - Provider SQLite para testes
- `FluentAssertions` - Assertions

### Testes de API
- `Microsoft.AspNetCore.Mvc.Testing` - WebApplicationFactory
- `Microsoft.EntityFrameworkCore.Sqlite` - Provider SQLite

## 📚 Recursos Adicionais

- [Documentação do xUnit](https://xunit.net/)
- [Documentação do Moq](https://github.com/moq/moq4)
- [Documentação do FluentAssertions](https://fluentassertions.com/)
- [Documentação do Coverlet](https://github.com/coverlet-coverage/coverlet)
- [Testes em ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/)

## 📄 Licença

Este projeto é um exemplo didático e pode ser usado livremente para fins educacionais.

## 🤝 Contribuindo

Este é um projeto didático. Sinta-se livre para usar como base para seus próprios projetos de aprendizado!

