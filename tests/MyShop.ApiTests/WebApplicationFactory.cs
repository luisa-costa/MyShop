using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyShop.API;
using MyShop.Infrastructure.Data;

namespace MyShop.ApiTests;

/// <summary>
/// Factory para criar uma instância da aplicação para testes.
/// 
/// WebApplicationFactory permite criar uma instância da aplicação ASP.NET Core
/// em memória para testes, sem precisar iniciar um servidor HTTP real.
/// 
/// Esta factory configura a aplicação para usar SQLite in-memory ao invés
/// do banco de dados de produção.
/// </summary>
public class MyShopWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Configura o ambiente de teste
        builder.UseEnvironment("Test");

        // Substitui serviços da aplicação para testes
        builder.ConfigureServices(services =>
        {
            // Remove o DbContext de produção
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<MyShopDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Cria uma conexão SQLite compartilhada que permanece aberta
            // Isso é necessário porque SQLite in-memory requer que a conexão
            // permaneça aberta para que o banco persista
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // Adiciona SQLite in-memory para testes usando a conexão compartilhada
            // IMPORTANTE: Passar a conexão diretamente garante que todos os contextos
            // usem a mesma conexão e, portanto, o mesmo banco em memória
            services.AddDbContext<MyShopDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });
        });

        // Garante que o banco seja criado após a aplicação ser construída
        builder.ConfigureServices(services =>
        {
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
            context.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection?.Dispose();
        }
        base.Dispose(disposing);
    }
}

