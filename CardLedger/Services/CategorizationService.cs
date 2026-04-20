namespace CardLedger.Services;

public interface ICategorizationService
{
    string CategorizeTransaction(string title);
}

public sealed class CategorizationService : ICategorizationService
{
    private readonly Dictionary<string, (string category, int priority)> _keywordRules;
    private readonly Dictionary<string, string> _synonyms;
    private const int MinimumSimilarityThreshold = 70;

    public CategorizationService()
    {
        _keywordRules = InitializeKeywordRules();
        _synonyms = InitializeSynonyms();
    }

    public string CategorizeTransaction(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "Não Categorizado";

        title = title.ToLower().Trim();

        // 0. Busca por parcelas (PRIMEIRA PRIORIDADE)
        var parcelaMatch = SearchParcelaPattern(title);
        if (!string.IsNullOrEmpty(parcelaMatch))
            return parcelaMatch;

        // 1. Busca por correspondência exata com keywords
        var exactMatch = SearchExactMatch(title);
        if (!string.IsNullOrEmpty(exactMatch))
            return exactMatch;

        // 2. Busca por múltiplas palavras (padrão de composto)
        var multiWordMatch = SearchMultiWordPattern(title);
        if (!string.IsNullOrEmpty(multiWordMatch))
            return multiWordMatch;

        // 3. Busca por similaridade fuzzy (tolerância a variações)
        var fuzzyMatch = SearchFuzzyMatch(title);
        if (!string.IsNullOrEmpty(fuzzyMatch))
            return fuzzyMatch;

        // 4. Análise de padrões especiais
        var patternMatch = SearchPatternMatch(title);
        if (!string.IsNullOrEmpty(patternMatch))
            return patternMatch;

        return "Não Categorizado";
    }

    private string? SearchParcelaPattern(string title)
    {
        // Detecta padrão de parcela: "Parcela X/Y" ou "parcela X/Y"
        // Exemplo: "KaBuM! - NuPay - Parcela 1/5" → "Parcelado"
        var parcelaPattern = @"[Pp]arcela\s+\d+\s*/\s*\d+";
        if (System.Text.RegularExpressions.Regex.IsMatch(title, parcelaPattern))
            return "Parcelado";

        return null;
    }

    private string? SearchExactMatch(string title)
    {
        // Busca ordenada por prioridade (palavras mais específicas primeiro)
        var matches = _keywordRules
            .Where(kvp => ContainsKeyword(title, kvp.Key))
            .OrderByDescending(kvp => kvp.Value.priority)
            .ThenByDescending(kvp => kvp.Key.Length)
            .ToList();

        return matches.Any() ? matches.First().Value.category : null;
    }

    private static bool ContainsKeyword(string title, string keyword)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(
            title,
            $@"(?<!\w){System.Text.RegularExpressions.Regex.Escape(keyword)}(?!\w)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private string? SearchMultiWordPattern(string title)
    {
        // Busca por padrões de múltiplas palavras
        var patterns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "mercado livre", "Compras Online" },
            { "pag seguro", "Compras Online" },
            { "picpay", "Transporte" },
            { "nubank", "Utilidades" },
            { "itau", "Utilidades" },
            { "bradesco", "Utilidades" },
            { "caixa", "Utilidades" },
            { "santander", "Utilidades" },
        };

        foreach (var pattern in patterns)
        {
            if (title.Contains(pattern.Key, StringComparison.OrdinalIgnoreCase))
                return pattern.Value;
        }

        return null;
    }

    private string? SearchFuzzyMatch(string title)
    {
        // Fuzzy matching com limite de similaridade
        var words = title.Split(new[] { ' ', '-', '/', '.' }, StringSplitOptions.RemoveEmptyEntries);

        var bestMatch = new { category = "", similarity = 0, priority = 0 };

        foreach (var word in words)
        {
            foreach (var rule in _keywordRules)
            {
                var similarity = CalculateSimilarity(word, rule.Key);

                if (similarity >= MinimumSimilarityThreshold)
                {
                    if (similarity > bestMatch.similarity || 
                        (similarity == bestMatch.similarity && rule.Value.priority > bestMatch.priority))
                    {
                        bestMatch = new { rule.Value.category, similarity, rule.Value.priority };
                    }
                }
            }
        }

        return bestMatch.similarity > 0 ? bestMatch.category : null;
    }

    private string? SearchPatternMatch(string title)
    {
        // Busca por padrões especiais
        if (title.Contains("aplic") || ContainsWord(title, "app")) return "Assinatura";
        if (ContainsWord(title, "banco") || ContainsWord(title, "transferência")) return "Utilidades";
        if (ContainsWord(title, "boleto") || ContainsWord(title, "pix")) return "Utilidades";
        if (ContainsWord(title, "jogo") || ContainsWord(title, "game")) return "Diversão";
        if (ContainsWord(title, "manutenção") || ContainsWord(title, "reparo")) return "Casa";
        if (ContainsWord(title, "limpeza") || ContainsWord(title, "material")) return "Casa";

        return null;
    }

    private static bool ContainsWord(string title, string term)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(title, $@"\b{System.Text.RegularExpressions.Regex.Escape(term)}\b");
    }

    private int CalculateSimilarity(string source, string target)
    {
        // Algoritmo de Levenshtein Distance para medir similaridade
        if (string.Equals(source, target, StringComparison.OrdinalIgnoreCase))
            return 100;

        int maxLength = Math.Max(source.Length, target.Length);
        if (maxLength == 0)
            return 100;

        int distance = LevenshteinDistance(source.ToLower(), target.ToLower());
        return (int)((1.0 - ((double)distance / maxLength)) * 100);
    }

    private int LevenshteinDistance(string source, string target)
    {
        var sourceLength = source.Length;
        var targetLength = target.Length;
        var matrix = new int[sourceLength + 1, targetLength + 1];

        for (int i = 0; i <= sourceLength; i++)
            matrix[i, 0] = i;

        for (int j = 0; j <= targetLength; j++)
            matrix[0, j] = j;

        for (int i = 1; i <= sourceLength; i++)
        {
            for (int j = 1; j <= targetLength; j++)
            {
                int cost = source[i - 1] == target[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(
                        matrix[i - 1, j] + 1,
                        matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[sourceLength, targetLength];
    }

    private Dictionary<string, (string, int)> InitializeKeywordRules()
    {
        return new(StringComparer.OrdinalIgnoreCase)
        {
            // Alimentação (prioridade alta)
            { "restaurante", ("Alimentação", 10) },
            { "pizzaria", ("Alimentação", 10) },
            { "hamburgueria", ("Alimentação", 10) },
            { "churrascaria", ("Alimentação", 10) },
            { "cantina", ("Alimentação", 9) },
            { "boteco", ("Alimentação", 9) },
            { "bar", ("Alimentação", 8) },

            // Supermercados e mercearias
            { "supermercado", ("Alimentação", 10) },
            { "mercado", ("Alimentação", 9) },
            { "padaria", ("Alimentação", 10) },
            { "açougue", ("Alimentação", 10) },
            { "hortifruti", ("Alimentação", 9) },
            { "feira", ("Alimentação", 8) },
            { "carrefour", ("Alimentação", 10) },
            { "atacadao", ("Alimentação", 10) },
            { "atacadão", ("Alimentação", 10) },
            { "assai", ("Alimentação", 10) },
            { "assaí", ("Alimentação", 10) },

            // Cafés e bebidas
            { "café", ("Alimentação", 9) },
            { "cafeteria", ("Alimentação", 9) },
            { "lanchonete", ("Alimentação", 9) },
            { "sorveteria", ("Alimentação", 9) },
            { "açaí", ("Alimentação", 9) },
            { "suco", ("Alimentação", 8) },
            { "smoothie", ("Alimentação", 8) },

            // Transporte (prioridade alta)
            { "uber", ("Transporte", 10) },
            { "uber eats", ("Alimentação", 10) },
            { "ifood", ("Alimentação", 10) },
            { "ifd*", ("Alimentação", 10) },
            { "ifd", ("Alimentação", 9) },
            { "99", ("Transporte", 10) },
            { "99pop", ("Transporte", 10) },
            { "taxi", ("Transporte", 10) },
            { "táxi", ("Transporte", 10) },
            { "bolt", ("Transporte", 10) },
            { "loggi", ("Transporte", 9) },
            { "passagem", ("Transporte", 9) },
            { "ônibus", ("Transporte", 9) },
            { "metrô", ("Transporte", 9) },
            { "trem", ("Transporte", 9) },

            // Combustível e estacionamento
            { "combustível", ("Transporte", 10) },
            { "gasolina", ("Transporte", 10) },
            { "diesel", ("Transporte", 10) },
            { "etanol", ("Transporte", 10) },
            { "shell", ("Transporte", 9) },
            { "esso", ("Transporte", 9) },
            { "br", ("Transporte", 8) },
            { "ipiranga", ("Transporte", 9) },
            { "estacionamento", ("Transporte", 10) },
            { "garagem", ("Transporte", 9) },
            { "manobrista", ("Transporte", 9) },

            // Assinaturas & Contas
            { "youtube", ("Assinaturas & Contas", 10) },
            { "netflix", ("Assinaturas & Contas", 10) },
            { "spotify", ("Assinaturas & Contas", 10) },
            { "prime", ("Assinaturas & Contas", 10) },
            { "amazon prime", ("Assinaturas & Contas", 10) },
            { "disney+", ("Assinaturas & Contas", 10) },
            { "hbo", ("Assinaturas & Contas", 10) },
            { "globoplay", ("Assinaturas & Contas", 10) },
            { "paramount", ("Assinaturas & Contas", 10) },
            { "apple tv", ("Assinaturas & Contas", 10) },
            { "crunchyroll", ("Assinaturas & Contas", 10) },
            { "google one", ("Assinaturas & Contas", 10) },
            { "google storage", ("Assinaturas & Contas", 10) },
            { "premium", ("Assinaturas & Contas", 7) },
            { "subscription", ("Assinaturas & Contas", 8) },
            { "plano", ("Assinaturas & Contas", 6) },

            // Compras Online
            { "shopee", ("Compras Online", 10) },
            { "amazon", ("Compras Online", 10) },
            { "aliexpress", ("Compras Online", 10) },
            { "mercado livre", ("Compras Online", 10) },
            { "ebay", ("Compras Online", 10) },
            { "wish", ("Compras Online", 10) },
            { "alibabaexpress", ("Compras Online", 10) },
            { "shein", ("Compras Online", 10) },
            { "banggood", ("Compras Online", 9) },
            { "fasttech", ("Compras Online", 9) },
            { "gearbest", ("Compras Online", 9) },

            // Saúde e farmácia
            { "farmácia", ("Saúde", 10) },
            { "farmacia", ("Saúde", 10) },
            { "manipulação", ("Saúde", 10) },
            { "drogaria", ("Saúde", 10) },
            { "hospital", ("Saúde", 10) },
            { "clínica", ("Saúde", 10) },
            { "clinica", ("Saúde", 10) },
            { "médico", ("Saúde", 10) },
            { "medico", ("Saúde", 10) },
            { "consultório", ("Saúde", 10) },
            { "consultorio", ("Saúde", 10) },
            { "odontológo", ("Saúde", 10) },
            { "dentista", ("Saúde", 10) },
            { "oftalmologista", ("Saúde", 10) },
            { "psicólogo", ("Saúde", 10) },
            { "psicologo", ("Saúde", 10) },
            { "vacina", ("Saúde", 9) },
            { "exame", ("Saúde", 9) },
            { "ressonância", ("Saúde", 9) },
            { "tomografia", ("Saúde", 9) },

            // Educação
            { "udemy", ("Educação", 10) },
            { "coursera", ("Educação", 10) },
            { "escola", ("Educação", 10) },
            { "universidade", ("Educação", 10) },
            { "faculdade", ("Educação", 10) },
            { "curso", ("Educação", 9) },
            { "professor", ("Educação", 9) },
            { "aula", ("Educação", 8) },
            { "reforço", ("Educação", 9) },
            { "tutoria", ("Educação", 9) },
            { "lingvo", ("Educação", 9) },
            { "duolingo", ("Educação", 9) },

            // Utilidades e serviços
            { "energia", ("Utilidades", 10) },
            { "água", ("Utilidades", 10) },
            { "internet", ("Utilidades", 10) },
            { "telefone", ("Utilidades", 10) },
            { "celular", ("Utilidades", 10) },
            { "conta", ("Utilidades", 8) },
            { "nuvem", ("Utilidades", 9) },
            { "hospedagem", ("Utilidades", 9) },
            { "dominio", ("Utilidades", 9) },
            { "vpn", ("Utilidades", 9) },
            { "seguro", ("Utilidades", 9) },

            // Casa e moradia
            { "imobiliaria", ("Casa", 10) },
            { "imobiliária", ("Casa", 10) },
            { "aluguel", ("Casa", 10) },
            { "condominio", ("Casa", 10) },
            { "condomínio", ("Casa", 10) },
            { "construção", ("Casa", 10) },
            { "construcao", ("Casa", 10) },
            { "reforma", ("Casa", 9) },
            { "pintura", ("Casa", 9) },
            { "carpintaria", ("Casa", 9) },
            { "encanamento", ("Casa", 9) },
            { "eletricista", ("Casa", 9) },
            { "limpeza", ("Casa", 8) },
            { "faxina", ("Casa", 8) },

            // Manutenção de veículo
            { "oficina", ("Transporte", 10) },
            { "mecânica", ("Transporte", 10) },
            { "mecanica", ("Transporte", 10) },
            { "pneu", ("Transporte", 9) },
            { "bateria", ("Transporte", 9) },
            { "óleo", ("Transporte", 9) },
            { "oleo", ("Transporte", 9) },
            { "filtro", ("Transporte", 9) },
            { "reparo", ("Transporte", 9) },
            { "manutenção", ("Transporte", 9) },
            { "manutencao", ("Transporte", 9) },

            // Roupas e moda
            { "loja de roupas", ("Vestuário", 10) },
            { "loja", ("Vestuário", 7) },
            { "moda", ("Vestuário", 9) },
            { "roupa", ("Vestuário", 9) },
            { "sapato", ("Vestuário", 9) },
            { "calçado", ("Vestuário", 9) },
            { "bolsa", ("Vestuário", 9) },
            { "cinto", ("Vestuário", 8) },
            { "acessório", ("Vestuário", 8) },
            { "acessorio", ("Vestuário", 8) },

            // Lazer e diversão
            { "cinema", ("Diversão", 10) },
            { "ingresso", ("Diversão", 10) },
            { "teatro", ("Diversão", 10) },
            { "show", ("Diversão", 10) },
            { "parque", ("Diversão", 9) },
            { "museu", ("Diversão", 9) },
            { "diversão", ("Diversão", 10) },
            { "diversao", ("Diversão", 10) },
            { "jogo", ("Diversão", 9) },
            { "game", ("Diversão", 9) },
            { "console", ("Diversão", 9) },
            { "playstation", ("Diversão", 9) },
            { "xbox", ("Diversão", 9) },
            { "nintendo", ("Diversão", 9) },

            // Beleza e cuidados
            { "beleza", ("Beleza", 10) },
            { "salão", ("Beleza", 10) },
            { "salon", ("Beleza", 10) },
            { "cabelo", ("Beleza", 10) },
            { "cabelereiro", ("Beleza", 10) },
            { "cabeleireiro", ("Beleza", 10) },
            { "barbaria", ("Beleza", 10) },
            { "barbearia", ("Beleza", 10) },
            { "barba", ("Beleza", 9) },
            { "manicure", ("Beleza", 10) },
            { "pedicure", ("Beleza", 10) },
            { "depilação", ("Beleza", 10) },
            { "depilacao", ("Beleza", 10) },
            { "cosmetologia", ("Beleza", 10) },
            { "estética", ("Beleza", 10) },
            { "estetica", ("Beleza", 10) },
            { "maquiagem", ("Beleza", 10) },
            { "maquiage", ("Beleza", 10) },
            { "perfume", ("Beleza", 9) },
            { "cosmetico", ("Beleza", 9) },
            { "cosmético", ("Beleza", 9) },

            // Esportes e academia
            { "academia", ("Esportes", 10) },
            { "musculação", ("Esportes", 10) },
            { "musculacao", ("Esportes", 10) },
            { "pilates", ("Esportes", 10) },
            { "yoga", ("Esportes", 10) },
            { "esporte", ("Esportes", 10) },
            { "desporto", ("Esportes", 10) },
            { "natação", ("Esportes", 10) },
            { "natacao", ("Esportes", 10) },
            { "futebol", ("Esportes", 10) },
            { "tênis", ("Esportes", 10) },
            { "tenis", ("Esportes", 10) },
            { "dança", ("Esportes", 10) },
            { "danca", ("Esportes", 10) },
            { "lutas", ("Esportes", 10) },
            { "mma", ("Esportes", 10) },
            { "treino", ("Esportes", 10) },
        };
    }

    private Dictionary<string, string> InitializeSynonyms()
    {
        return new(StringComparer.OrdinalIgnoreCase)
        {
            { "msg", "restaurante" },
            { "comida", "alimentação" },
            { "marmita", "alimentação" },
            { "delivery", "alimentação" },
            { "ifood", "uber eats" },
            { "abastecimento", "gasolina" },
            { "bombom", "café" },
            { "cerveja", "bar" },
            { "refrigerante", "alimentação" },
            { "água", "utilidades" },
            { "eletricidade", "energia" },
            { "gás", "utilidades" },
        };
    }
}
