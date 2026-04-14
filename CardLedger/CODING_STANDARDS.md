# CODING_STANDARDS.md - Padrões de Desenvolvimento CardLedger

## 🔒 Classes com `sealed` (Obrigatório)

Todas as classes que **NÃO são herdadas** DEVEM usar `sealed`.

### ✅ Correto
```csharp
public sealed class TransactionService : ITransactionService { }
public sealed class Transaction { }
public sealed class MonthlyInvoice { }
```

### ❌ Errado
```csharp
public class TransactionService : ITransactionService { }  // Falta sealed
public class Transaction { }  // Falta sealed
```

### Exceções (SEM `sealed`)

| Tipo | Motivo |
|------|--------|
| **Controllers** | Padrão ASP.NET |
| **DbContext** | Padrão EF Core |
| **Classes Abstratas** | São bases para herança |
| **Classes Especializadas** | São subclasses de outras |

### Por quê?
- **Performance:** Permite devirtualization e melhor inlining
- **Segurança:** Evita herança não intencional
- **Clareza:** Documenta que a classe é "final"

---

Para mais contexto arquitetural, consulte `AGENTS.md`.

