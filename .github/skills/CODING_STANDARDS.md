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

## 🧹 Linhas em Branco (Obrigatório)

Não deixar linhas em branco desnecessárias nos arquivos de código.

### ✅ Correto
- Sem linha em branco no final do arquivo
- A última linha do arquivo deve ser uma linha com conteúdo (sem quebra de linha extra após ela)
- Sem múltiplas linhas em branco consecutivas
- Sem linhas em branco separando trechos que pertencem ao mesmo bloco lógico

### ❌ Errado
- Arquivo terminando com linha em branco
- Arquivo com quebra de linha extra após a última linha de conteúdo
- Duas ou mais linhas em branco em sequência

### Por quê?
- **Consistência:** Padroniza a formatação entre todos os arquivos
- **Legibilidade:** Evita ruído visual desnecessário
- **Revisão de código:** Reduz diffs de formatação

---

Para mais contexto arquitetural, consulte `AGENTS.md`.

