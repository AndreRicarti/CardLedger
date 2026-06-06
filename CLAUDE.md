# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build

# Run (from CardLedger/)
dotnet run --project CardLedger

# Run all tests
dotnet test

# Run a single test
dotnet test --filter "FullyQualifiedName~GetTransactionsByCategoryAsync"

# EF migrations (from CardLedger/)
dotnet ef migrations add <Name>
dotnet ef database update
```

## Architecture

Two projects: `CardLedger` (Web API) and `CardLedger.Tests` (xUnit).

**Request flow:** Controller → Service → `InvoiceDbContext` (EF Core + PostgreSQL via Npgsql)

**Services:**
- `CsvParserService` — parses Nubank CSV exports; extracts the `InvoiceKey` (format `YYYY-MM`) from the filename by subtracting one month from the due date
- `CategorizationService` — auto-categorizes transaction titles using exact keyword matching, multi-word rules, and Levenshtein fuzzy matching; called by `CsvParserService` at parse time
- `InvoiceService` — import (replaces all transactions for the same `InvoiceKey` on re-import) and query by category
- `TransactionService` — manual category correction and category listing

**Models:**
- `Transaction` — has a persisted `CategoryId` FK and `[NotMapped]` `Category`/`CategoryName` string properties used only during CSV parsing before the FK is resolved
- `Category` — 12 categories seeded via `HasData` in `InvoiceDbContext.OnModelCreating`

**Key conventions:**
- All models and services use `sealed`
- File-scoped namespaces (`namespace CardLedger.Models;`)
- Tests use `InMemory` EF provider (not PostgreSQL)

## CI/CD

- **CI** (`ci.yml`): runs on every push/PR to `master` — restore, build (Release), test
- **CD** (`cd.yml`): triggers after CI passes on `master` — builds and pushes Docker image to GHCR (`ghcr.io/<owner>/cardledger-api:latest`)
- App runs on port `8080` inside Docker; TLS is handled externally by Nginx
- Swagger UI is served at the root (`/`) in all environments
