using Microsoft.EntityFrameworkCore;
using MyShop.Application.Interfaces;
using MyShop.Application.Services;
using MyShop.Infrastructure.Data;
using MyShop.Infrastructure.Repositories;
using MyShop.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "MyShop API",
        Version = "v1",
        Description = "API de exemplo para demonstração de testes em C#/.NET",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "MyShop Team"
        }
    });
});

// Configuração do Entity Framework Core
// Em produção, use uma connection string do appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=(localdb)\\mssqllocaldb;Database=MyShopDb;Trusted_Connection=True;MultipleActiveResultSets=true";

builder.Services.AddDbContext<MyShopDbContext>(options =>
    options.UseSqlServer(connectionString));

// Registro de repositórios (padrão Repository)
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Registro de services de aplicação
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<OrderService>();

// Registro de services de infraestrutura
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<IPaymentGateway, PaymentGateway>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MyShop API v1");
        c.RoutePrefix = string.Empty; // Swagger UI na raiz
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Torna a classe Program acessível para WebApplicationFactory em testes
// No .NET 6+ com top-level statements, precisamos expor a classe Program explicitamente
public partial class Program { }
