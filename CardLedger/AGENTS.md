# AGENTS.md - Arquitetura do CardLedger

## 📋 Visão Geral

CardLedger é uma API em C# .NET 10 com arquitetura **em camadas** baseada em **agentes de serviço**. Cada camada tem responsabilidades bem definidas e comunica-se através de interfaces.

```
┌─────────────────────────────────────────────────────────┐
│                    CLIENTE (HTTP)                       │
│              (Postman, Frontend, etc)                   │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│              CAMADA DE CONTROLE                         │
│  ┌──────────────────────────────────────────────────┐  │
│  │ Controllers                                      │  │
│  │ - InvoiceController                             │  │
│  │ - TransactionController                         │  │
│  └──────────────────────────────────────────────────┘  │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│              CAMADA DE SERVIÇO (AGENTES)               │
│  ┌──────────────────────────────────────────────────┐  │
│  │ IInvoiceService & InvoiceService                │  │
│  │ - Lógica de agrupamento por mês                 │  │
│  │ - Cálculo de totais                             │  │
│  │ - Resumos por categoria                         │  │
│  └──────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────┐  │
│  │ ITransactionService & TransactionService        │  │
│  │ - Filtros avançados                             │  │
│  │ - Atualização de categorias                     │  │
│  └──────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────┐  │
│  │ ICsvParserService & CsvParserService            │  │
│  │ - Parse de CSV do Nubank                        │  │
│  │ - Categorização automática                      │  │
│  │ - Detecção de estornos                          │  │
│  └──────────────────────────────────────────────────┘  │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│              CAMADA DE DADOS                           │
│  ┌──────────────────────────────────────────────────┐  │
│  │ InvoiceDbContext (Entity Framework Core)        │  │
│  │ - DbSet<Transaction>                            │  │
│  │ - Índices otimizados                            │  │
│  └──────────────────────────────────────────────────┘  │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│              BANCO DE DADOS                            │
│              SQLite (invoices.db)                      │
└─────────────────────────────────────────────────────────┘
```

---

## 🤖 Agentes (Serviços)

### 1. **CsvParserService** - Agente de Importação
**Arquivo:** `Services/CsvParserService.cs`

**Responsabilidade:** Transformar arquivo CSV do Nubank em objetos `Transaction`

**Métodos Principais:**
```csharp
Task<List<Transaction>> ParseNubankCsvAsync(Stream fileStream)
```

**Funcionalidades:**
- ✅ Lê CSV do Nubank (formato: `date,title,amount`)
- ✅ Parsing robusto com suporte a valores negativos (estornos)
- ✅ Categorização automática baseada em palavras-chave
- ✅ Extrai ano e mês da data
- ✅ Marca transações como estorno quando `amount < 0`

**Regras de Categorização:**
- **Alimentação**: restaurante, pizzaria, supermercado, padaria, etc.
- **Transporte**: uber, 99, taxi, gasolina, estacionamento, etc.
- **Assinatura**: youtube, netflix, spotify, amazon prime, etc.
- **Compras Online**: shopee, amazon, aliexpress, mercado livre, etc.
- **Saúde**: farmácia, hospital, clínica, médico, etc.
- **Educação**: udemy, coursera, escola, faculdade, etc.
- **Utilidades**: energia, água, internet, telefone, etc.
- **Não Categorizado**: padrão para transações sem categoria identificada

**Entrada:** `Stream` (arquivo CSV)
**Saída:** `List<Transaction>`

---

### 2. **InvoiceService** - Agente de Análise
**Arquivo:** `Services/InvoiceService.cs`

**Responsabilidade:** Organizar e analisar transações em faturas mensais

**Métodos Principais:**
```csharp
Task<List<MonthlyInvoice>> GetAllInvoicesAsync()
Task<MonthlyInvoice?> GetMonthlyInvoiceAsync(int year, int month)
Task<MonthlySummary> GetMonthlySummaryAsync(int year, int month)
Task<int> ImportTransactionsAsync(List<Transaction> transactions)
```

**Funcionalidades:**
- ✅ Agrupa transações por mês e ano
- ✅ Calcula total gasto (sem estornos)
- ✅ Calcula total de estornos (refunds)
- ✅ Calcula total líquido (gasto - estornos)
- ✅ Conta quantidade de transações
- ✅ Evita duplicatas ao importar
- ✅ Gera resumos por categoria com percentuais

**Entrada:** `List<Transaction>`
**Saída:** `MonthlyInvoice`, `MonthlySummary`

---

### 3. **TransactionService** - Agente de Filtros
**Arquivo:** `Services/TransactionService.cs`

**Responsabilidade:** Consultas e atualizações em transações individuais

**Métodos Principais:**
```csharp
Task<List<Transaction>> FilterTransactionsAsync(int? year, int? month, string? category)
Task<Transaction?> GetTransactionAsync(int id)
Task<bool> UpdateCategoryAsync(int id, string category)
```

**Funcionalidades:**
- ✅ Filtros por ano, mês e categoria (combinados)
- ✅ Recuperação de transação individual por ID
- ✅ Atualização manual de categorias
- ✅ Resultados ordenados por data (mais recentes primeiro)

**Entrada:** Parâmetros de filtro ou ID
**Saída:** `List<Transaction>`, `Transaction`

---

## 🔄 Fluxo de Dados

### **Caso de Uso 1: Importar Fatura CSV**

```
1. Cliente faz POST /api/invoice/import com arquivo CSV
   ↓
2. InvoiceController recebe a requisição
   ↓
3. CsvParserService.ParseNubankCsvAsync() converte CSV → List<Transaction>
   ↓
4. InvoiceService.ImportTransactionsAsync() valida e salva no BD
   ↓
5. Retorna ImportResponse com total importado e meses afetados
```

---

### **Caso de Uso 2: Consultar Fatura do Mês**

```
1. Cliente faz GET /api/invoice/2026/4
   ↓
2. InvoiceController chama InvoiceService.GetMonthlyInvoiceAsync(2026, 4)
   ↓
3. InvoiceService busca todas as transações do período no BD
   ↓
4. Agrupa dados e calcula totais
   ↓
5. Retorna MonthlyInvoice com resumo do mês
```

---

### **Caso de Uso 3: Filtrar Transações com Critérios**

```
1. Cliente faz GET /api/transaction?year=2026&month=4&category=Alimentação
   ↓
2. TransactionController chama TransactionService.FilterTransactionsAsync()
   ↓
3. TransactionService monta query com filtros
   ↓
4. Retorna List<Transaction> ordenado por data
```

---

## 📦 Modelos de Dados

### **Transaction** (Entidade)
```csharp
public class Transaction
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string Title { get; set; }           // Ex: "Ec *Shellbox"
    public decimal Amount { get; set; }         // Sempre positivo
    public string Category { get; set; }        // Ex: "Compras Online"
    public string Source { get; set; }          // Ex: "nubank"
    public int Year { get; set; }               // 2026
    public int Month { get; set; }              // 4
    public bool IsRefund { get; set; }          // true se estorno
    public DateTime CreatedAt { get; set; }     // Data de importação
}
```

### **MonthlyInvoice** (Resumo)
```csharp
public class MonthlyInvoice
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; }       // "April 2026"
    public decimal TotalSpent { get; set; }     // Sem estornos
    public decimal TotalRefunds { get; set; }   // Estornos
    public decimal NetTotal { get; set; }       // TotalSpent - TotalRefunds
    public int TransactionCount { get; set; }
}
```

### **CategorySummary** (Por Categoria)
```csharp
public class CategorySummary
{
    public string Category { get; set; }
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }     // 0-100
}

public class MonthlySummary
{
    public decimal Total { get; set; }
    public List<CategorySummary> ByCategory { get; set; }
}
```

---

## 🛣️ Rotas (Endpoints)

| Método | Rota | Agente | Descrição |
|--------|------|--------|-----------|
| **POST** | `/api/invoice/import` | CsvParserService + InvoiceService | Importar CSV do Nubank |
| **GET** | `/api/invoice` | InvoiceService | Listar todas as faturas (agrupadas por mês) |
| **GET** | `/api/invoice/{year}/{month}` | InvoiceService | Fatura de um mês específico |
| **GET** | `/api/invoice/{year}/{month}/summary` | InvoiceService | Resumo por categoria do mês |
| **GET** | `/api/transaction` | TransactionService | Filtrar transações (year, month, category) |
| **GET** | `/api/transaction/{id}` | TransactionService | Obter transação específica |
| **PATCH** | `/api/transaction/{id}/category` | TransactionService | Atualizar categoria manualmente |

---

## 🗄️ Banco de Dados

**Tipo:** SQLite (arquivo local `invoices.db`)

**Tabela:** `Transactions`

**Índices:**
- Índice composto em `(Year, Month)` - para consultas por período
- Índice em `Category` - para filtros por categoria
- Índice em `Date` - para ordenação

---

## ⚙️ Injeção de Dependência

**Configurado em `Program.cs`:**

```csharp
// DbContext
builder.Services.AddDbContext<InvoiceDbContext>(options =>
    options.UseSqlite("Data Source=invoices.db"));

// Serviços
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ICsvParserService, CsvParserService>();
```

**Padrão:** Injeção por interface (permite testes e mocking)

---

## 🧪 Testabilidade

Cada serviço é testável graças a:

1. **Interfaces bem definidas** - Fácil criar mocks
2. **Dependency Injection** - Desacoplamento de dependências
3. **Métodos async/await** - Facilita testes assíncronos
4. **Lógica de negócio isolada** - Serviços não conhecem HTTP

---

## 🚀 Como Executar

```bash
# Restaurar dependências
dotnet restore

# Executar
dotnet run

# Swagger disponível em
http://localhost:5244/swagger
```

---

## 📝 Exemplo de Fluxo Completo

### 1. Importar arquivo CSV
```http
POST http://localhost:5244/api/invoice/import?source=nubank
Content-Type: multipart/form-data

file=@nubank_fatura.csv
```

**Resposta:**
```json
{
  "imported": 11,
  "months": ["03/2026", "04/2026"]
}
```

### 2. Consultar fatura de abril/2026
```http
GET http://localhost:5244/api/invoice/2026/4
```

**Resposta:**
```json
{
  "year": 2026,
  "month": 4,
  "monthName": "April 2026",
  "totalSpent": 1226.76,
  "totalRefunds": 58.97,
  "netTotal": 1167.79,
  "transactionCount": 11
}
```

### 3. Obter resumo por categoria
```http
GET http://localhost:5244/api/invoice/2026/4/summary
```

**Resposta:**
```json
{
  "total": 1167.79,
  "byCategory": [
    { "category": "Compras Online", "amount": 926.86, "percentage": 79.4 },
    { "category": "Assinatura", "amount": 53.90, "percentage": 4.6 },
    { "category": "Alimentação", "amount": 186.03, "percentage": 15.9 }
  ]
}
```

### 4. Filtrar transações de alimentação
```http
GET http://localhost:5244/api/transaction?year=2026&month=4&category=Alimentação
```

**Resposta:**
```json
[
  {
    "id": 5,
    "date": "2026-04-15",
    "title": "Ec *Shellbox",
    "amount": 100.00,
    "category": "Compras Online",
    "source": "nubank",
    "year": 2026,
    "month": 4,
    "isRefund": false,
    "createdAt": "2026-04-20T10:30:00Z"
  }
]
```

### 5. Atualizar categoria manualmente
```http
PATCH http://localhost:5244/api/transaction/5/category
Content-Type: application/json

"Alimentação"
```

---

## 📌 Notas Importantes

- **Categorização:** A categorização automática usa matching de strings. Pode ser expandida com ML no futuro.
- **Estornos:** Valores negativos no CSV são automaticamente detectados como estornos (`IsRefund = true`).
- **Duplicatas:** O sistema evita importações duplicadas comparando data, título e valor.
- **Timezone:** Todas as datas são em UTC (`DateTime.UtcNow`).
- **Escalabilidade:** Com mais transações, considere adicionar paginação nos endpoints de filtro.

---

## 🔐 Segurança

- ✅ Validação de entrada em todas as rotas
- ✅ CORS configurado para aceitar todas as origens (ajustar em produção)
- ✅ Sem autenticação por padrão (adicionar se necessário)
- ✅ Sem validação de arquivo por padrão (adicionar se necessário)

---

**Última atualização:** 2026  
**Versão .NET:** 10.0  
**Banco de Dados:** SQLite
