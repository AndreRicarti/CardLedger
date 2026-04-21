using CardLedger.Data;
using CardLedger.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

// SQLite aponta para o volume montado em /app/data
var dbPath = Path.Combine("data", "invoices.db");
builder.Services.AddDbContext<InvoiceDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Registrar serviços
builder.Services.AddScoped<ICategorizationService, CategorizationService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ICsvParserService, CsvParserService>();

var app = builder.Build();

// Reset controlado do banco: recria apenas uma vez (quando ainda não estiver normalizado)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();

    var appliedMigrations = (await db.Database.GetAppliedMigrationsAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
    var isNormalized = appliedMigrations.Contains("20260421225427_NormalizeCategories");

    if (!isNormalized)
    {
        await db.Database.EnsureDeletedAsync();
    }

    await db.Database.MigrateAsync();
}

// Swagger disponível em todos os ambientes (útil no ZimaOS)
app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "CardLedger API v2");
    options.RoutePrefix = string.Empty;
});

// HTTPS removido — TLS é responsabilidade do Nginx
app.UseAuthorization();
app.MapControllers();

app.Run();