using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyShop.Infrastructure.Data;

namespace MyShop.IntegrationTests;

/// <summary>
/// Fixture que configura o banco de dados em memória para os testes de integração.
/// 
/// Uma Fixture no xUnit é uma classe que permite compartilhar setup/teardown
/// entre múltiplos testes. Isso é útil para configurar recursos caros, como
/// conexões de banco de dados.
/// 
/// IMPORTANTE: Esta fixture usa SQLite in-memory, que suporta migrations
/// e se comporta de forma mais próxima a um banco real do que o provider
/// Microsoft.EntityFrameworkCore.InMemory.
/// </summary>
public class DatabaseFixture : IDisposable
{
    private SqliteConnection? _connection;

    /// <summary>
    /// Cria um DbContext configurado para usar SQLite in-memory.
    /// Cada teste deve criar seu próprio contexto para garantir isolamento.
    /// </summary>
    public MyShopDbContext CreateContext()
    {
        // Cria uma nova conexão SQLite em memória para cada contexto
        // Isso garante isolamento entre testes
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<MyShopDbContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new MyShopDbContext(options);

        // Em um cenário real, você teria migrations criadas com:
        // dotnet ef migrations add InitialCreate --project src/MyShop.Infrastructure
        // Por simplicidade, usamos EnsureCreated aqui
        context.Database.EnsureCreated();

        return context;
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}

