# Invoice API — Gestão de Faturas de Cartão

API em C# .NET 8 para importar, organizar e analisar faturas de cartão de crédito (Nubank, Inter, etc.)

## Estrutura do projeto

```
InvoiceApi/
├── Controllers/
│   ├── InvoiceController.cs       # Upload, listagem mensal, resumo
│   └── TransactionController.cs   # Filtros, categorização
├── Services/
│   ├── InvoiceService.cs          # Lógica de agrupamento e consultas
│   └── CsvParserService.cs        # Parser do CSV do Nubank
├── Models/
│   ├── Transaction.cs             # Entidade transação
│   └── MonthlyInvoice.cs          # Fatura mensal (agrupada)
├── Data/
│   └── InvoiceDbContext.cs        # EF Core + SQLite
└── Program.cs
```

## Como rodar

```bash
dotnet restore
dotnet run
# Swagger disponível em http://localhost:5000/swagger
```

## Endpoints

### Importar fatura CSV do Nubank
```
POST /api/invoice/import?source=nubank
Content-Type: multipart/form-data
Body: file = nubank_fatura.csv
```
Resposta:
```json
{ "imported": 11, "months": ["03/2026", "04/2026"] }
```

### Listar todas as faturas (agrupadas por mês)
```
GET /api/invoice
```
Resposta:
```json
[
  { "year": 2026, "month": 4, "monthName": "April 2026",
    "totalSpent": 1226.76, "totalRefunds": 58.97,
    "netTotal": 1167.79, "transactionCount": 11 }
]
```

### Fatura de um mês específico
```
GET /api/invoice/2026/3
```

### Resumo por categoria
```
GET /api/invoice/2026/3/summary
```
Resposta:
```json
{
  "total": 1167.79,
  "byCategory": [
    { "category": "Compras Online", "amount": 926.86, "percentage": 79.4 },
    { "category": "Assinatura",     "amount": 53.90,  "percentage": 4.6 },
    ...
  ]
}
```

### Filtrar transações
```
GET /api/transaction?year=2026&month=3&category=Alimentação
```

### Atualizar categoria manualmente
```
PATCH /api/transaction/5/category
Body: "Alimentação"
```

## Formato CSV aceito (Nubank)

```csv
date,title,amount
2026-04-02,Ec *Shellbox,100
2026-03-31,Google Youtubepremium,53.9
2026-03-27,Estorno de "Shopee *Harbon",-58.97
```

> Estornos (valores negativos) são detectados automaticamente e contabilizados separadamente.

## Categorias automáticas

O `CsvParserService` detecta automaticamente:
- **Alimentação** — supermercado, ifood, market
- **Restaurante** — restaurante, lanche, burger
- **Compras Online** — shopee, amazon, mercado livre
- **Transporte** — uber, 99, cabify
- **Assinatura** — netflix, spotify, youtube, google
- **Combustível** — shell, petrobras, posto
- **Saúde** — farmácia, drogaria
- **Estorno** — estorno, devolução
- **Outros** — demais

Você pode corrigir manualmente via `PATCH /api/transaction/{id}/category`.
