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

// Criar/atualizar banco de dados automaticamente
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();

    var hasTransactionsTable = await db.Database
        .SqlQueryRaw<int>("""
            SELECT COUNT(1)
            FROM sqlite_master
            WHERE type = 'table' AND name = 'Transactions'
            """)
        .SingleAsync() > 0;

    var hasMigrationsHistoryTable = await db.Database
        .SqlQueryRaw<int>("""
            SELECT COUNT(1)
            FROM sqlite_master
            WHERE type = 'table' AND name = '__EFMigrationsHistory'
            """)
        .SingleAsync() > 0;

    if (hasTransactionsTable && !hasMigrationsHistoryTable)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            INSERT OR IGNORE INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ('20260416003519_InitialCreate', '10.0.6');
            """);
    }

    db.Database.Migrate();
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