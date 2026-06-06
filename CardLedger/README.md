# CardLedger API

API em .NET 10 para importar, organizar e analisar faturas de cartão de crédito Nubank.

## Configuração inicial

Após clonar o repositório, execute uma vez para ativar os git hooks (auto-incremento de versão):

```bash
git config core.hooksPath .githooks
```

## Como rodar

```bash
dotnet restore
dotnet run --project CardLedger
# Swagger disponível em http://localhost:5244
```

## Endpoints

### Importar fatura CSV
```
POST /api/invoice/import?source=nubank
Content-Type: multipart/form-data
Body: file = Nubank_2026-05-10.csv
```
Resposta:
```json
{ "imported": 42, "months": ["2026-04"] }
```

### Resumo da fatura por mês
```
GET /api/invoice/key/{invoiceKey}/summary
```
Resposta:
```json
{
  "invoiceKey": "2026-04",
  "monthName": "abril 2026",
  "totalSpent": 1226.76,
  "totalRefunds": 3.99,
  "netTotal": 1222.77,
  "transactionCount": 42,
  "categories": [
    { "category": "Alimentação", "amount": 300.00, "percentage": 24.5 }
  ]
}
```

### Transações agrupadas por categoria
```
GET /api/invoice/key/{invoiceKey}/transactions-by-category
GET /api/invoice/key/{invoiceKey}/transactions-by-category?category=Alimentação
```

### Alterar categoria de uma transação
```
PATCH /api/invoice/key/{invoiceKey}/transactions/{id}/category
Body: { "categoryId": 2 }
```

### Listar categorias disponíveis
```
GET /api/transaction/categories
```

### Alterar categoria por ID da transação
```
PATCH /api/transaction/{id}/category
Body: { "categoryId": 2 }
```

### Excluir fatura por InvoiceKey
```
DELETE /api/transaction/{invoiceKey}
```

## Formato CSV aceito (Nubank)

```csv
date,title,amount
2026-04-02,Ec *Shellbox,100.00
2026-03-31,Google Youtubepremium,53.90
2026-03-27,Estorno Shopee,-58.97
```

O `InvoiceKey` é extraído automaticamente do nome do arquivo (`Nubank_YYYY-MM-DD.csv`), subtraindo um mês da data de vencimento.

> Transações com título `Pagamento recebido` e `Valor pendente do mês anterior` são ignoradas na importação.

## Banco de dados

| Ambiente | Banco |
|---|---|
| Desenvolvimento | `card_ledger_dev` |
| Produção | `card_ledger` |

As migrations são aplicadas automaticamente na inicialização.
