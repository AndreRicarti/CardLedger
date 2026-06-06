using System.Reflection;
using CardLedger.Data;
using CardLedger.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
var version = (Assembly.GetExecutingAssembly()
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "1.0.0")
    .Split('+')[0];

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "CardLedger", Version = version });
});

builder.Services.AddDbContext<InvoiceDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registrar serviços
builder.Services.AddScoped<ICategorizationService, CategorizationService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ICsvParserService, CsvParserService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
    await db.Database.MigrateAsync();
}

// Swagger disponível em todos os ambientes (útil no ZimaOS)
app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", $"CardLedger {version}");
    options.RoutePrefix = string.Empty;
});

// HTTPS removido — TLS é responsabilidade do Nginx
app.UseAuthorization();
app.MapControllers();

app.Run();