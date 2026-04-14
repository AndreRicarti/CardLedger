using System.Globalization;
using CardLedger.Models;

namespace CardLedger.Services
{
    public interface ICsvParserService
    {
        Task<List<Transaction>> ParseNubankCsvAsync(Stream fileStream);
    }

    public class CsvParserService : ICsvParserService
    {
        private readonly Dictionary<string, string> _categoryRules;

        public CsvParserService()
        {
            _categoryRules = InitializeCategoryRules();
        }

        public async Task<List<Transaction>> ParseNubankCsvAsync(Stream fileStream)
        {
            var transactions = new List<Transaction>();

            using (var reader = new StreamReader(fileStream))
            {
                // Pula o header (date,title,amount)
                var header = await reader.ReadLineAsync();

                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var parts = ParseCsvLine(line);
                    if (parts.Count < 3)
                        continue;

                    if (DateOnly.TryParse(parts[0], out var date) && 
                        decimal.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
                    {
                        var title = parts[1];
                        var isRefund = amount < 0;
                        var category = CategorizeTransaction(title);

                        transactions.Add(new Transaction
                        {
                            Date = date,
                            Title = title,
                            Amount = Math.Abs(amount),
                            Category = category,
                            Source = "nubank",
                            Year = date.Year,
                            Month = date.Month,
                            IsRefund = isRefund
                        });
                    }
                }
            }

            return transactions;
        }

        private List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = string.Empty;
            var inQuotes = false;

            foreach (var c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.Trim('"').Trim());
                    current = string.Empty;
                }
                else
                {
                    current += c;
                }
            }

            result.Add(current.Trim('"').Trim());
            return result;
        }

        private string CategorizeTransaction(string title)
        {
            title = title.ToLower();

            foreach (var rule in _categoryRules)
            {
                if (title.Contains(rule.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return rule.Value;
                }
            }

            return "Não Categorizado";
        }

        private Dictionary<string, string> InitializeCategoryRules()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Alimentação
                { "restaurante", "Alimentação" },
                { "pizzaria", "Alimentação" },
                { "hamburgueria", "Alimentação" },
                { "supermercado", "Alimentação" },
                { "padaria", "Alimentação" },
                { "mercado", "Alimentação" },
                { "açougue", "Alimentação" },
                { "cafe", "Alimentação" },

                // Transporte
                { "uber", "Transporte" },
                { "99", "Transporte" },
                { "taxi", "Transporte" },
                { "passagem", "Transporte" },
                { "combustível", "Transporte" },
                { "gasolina", "Transporte" },
                { "estacionamento", "Transporte" },

                // Assinatura
                { "youtube", "Assinatura" },
                { "netflix", "Assinatura" },
                { "spotify", "Assinatura" },
                { "amazon", "Assinatura" },
                { "prime", "Assinatura" },
                { "premium", "Assinatura" },
                { "subscription", "Assinatura" },

                // Compras Online
                { "shopee", "Compras Online" },
                { "amazon", "Compras Online" },
                { "aliexpress", "Compras Online" },
                { "mercado livre", "Compras Online" },
                { "ebay", "Compras Online" },
                { "wish", "Compras Online" },

                // Saúde
                { "farmácia", "Saúde" },
                { "hospital", "Saúde" },
                { "clínica", "Saúde" },
                { "médico", "Saúde" },
                { "odontológo", "Saúde" },

                // Educação
                { "udemy", "Educação" },
                { "coursera", "Educação" },
                { "escola", "Educação" },
                { "universidade", "Educação" },
                { "faculdade", "Educação" },

                // Utilidades
                { "energia", "Utilidades" },
                { "água", "Utilidades" },
                { "internet", "Utilidades" },
                { "telefone", "Utilidades" },
                { "conta", "Utilidades" },
            };
        }
    }
}
