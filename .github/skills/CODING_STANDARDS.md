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

## 📦 File Scoped Namespace (Obrigatório)

Todos os arquivos DEVEM usar **file scoped namespace** (C# 11+).

### ✅ Correto
```csharp
namespace CardLedger.Models;

public sealed class Transaction
{
    public int Id { get; set; }
    // propriedades...
}
```

### ❌ Errado
```csharp
namespace CardLedger.Models
{
    public sealed class Transaction
    {
        public int Id { get; set; }
        // propriedades...
    }
}
```

### Por quê?
- **Concisão:** Menos indentação
- **Legibilidade:** Código mais limpo
- **Moderno:** Padrão C# 11+
- **Consistência:** Uma classe por arquivo

---

Para mais contexto arquitetural, consulte `AGENTS.md`.

