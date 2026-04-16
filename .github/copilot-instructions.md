# Copilot Instructions

## Project Guidelines

### Arquitetura
Quando trabalhando no projeto CardLedger:
- Sempre consultar AGENTS.md como referência de arquitetura
- Define os 3 agentes: CsvParserService, InvoiceService, TransactionService
- Use como guia para mudanças arquiteturais, novas features e validações

### Padrões de Código
- **OBRIGATÓRIO:** Classes não herdadas usam sealed
- **OBRIGATÓRIO:** Usar file scoped namespace em todos os arquivos
- Ver CODING_STANDARDS.md para detalhes completos