using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
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

            // Adiciona SQLite in-memory para testes
            services.AddDbContext<MyShopDbContext>(options =>
            {
                options.UseSqlite("DataSource=:memory:");
            });

            // Garante que o banco seja criado
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
            context.Database.EnsureCreated();
        });
    }
}

