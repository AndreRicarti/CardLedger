using CardLedger.Services.Categorization;

namespace CardLedger.Services;

public sealed class CategorizationService : ICategorizationService
{
    private readonly IReadOnlyList<ICategoryRule> _rules = BuildRules();

    public string CategorizeTransaction(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "Não Categorizado";

        title = title.ToLower().Trim();

        CategoryMatch? best = null;

        foreach (var rule in _rules)
        {
            var match = rule.Match(title);
            if (match.HasValue && (best is null || match.Value.Priority > best.Value.Priority))
                best = match;
        }

        return best?.Category ?? "Não Categorizado";
    }

    private static IReadOnlyList<ICategoryRule> BuildRules()
    {
        return
        [
            new RegexCategoryRule(@"parcela\s+\d+\s*/\s*\d+", "Parcelado"),

            new KeywordCategoryRule("Carro",
            [
                ("park car one", 11), ("zona azul barueri", 11), ("zonaazulbarueri", 11), ("nutag", 11),
                ("shellbox", 11), ("rei tupa", 11)
            ]),

            new KeywordCategoryRule("Games",
            [
                ("steam", 11), ("greenman", 11)
            ]),

            new KeywordCategoryRule("Mayara",
            [
                ("shopee", 10), ("shein *shein.com", 11)
            ]),

            new KeywordCategoryRule("Assinaturas & Contas",
            [
                ("conta vivo", 11), ("github", 10), ("ig*floraenergia", 11),
                ("enel", 11), ("enelsp", 11), ("flexpag", 11), ("fmu", 11),
                ("sabesp", 11), ("melimais", 11), ("alares", 11),
                ("youtubepremium", 11), ("youtube", 10), ("netflix", 10),
                ("spotify", 10), ("amazon prime", 10), ("amazonprimebr", 11), ("disney+", 10),
                ("hbo", 10), ("globoplay", 10), ("paramount", 10), ("locaweb", 11),
                ("apple tv", 10), ("crunchyroll", 10), ("google one", 10),
                ("google storage", 10), ("premium", 7), ("subscription", 8), ("plano", 6),
                ("anthropic", 11)
            ]),

            new KeywordCategoryRule("Compras Avulsas",
            [
                ("aliexpress", 10), ("compra avulsa", 10), ("compras avulsas", 10),
                ("cinemark tambore", 11)
            ]),

            new KeywordCategoryRule("Compras Online",
            [
                ("amazon", 10), ("mercado livre", 10), ("ebay", 10), ("wish", 10),
                ("alibabaexpress", 10), ("shein", 10), ("banggood", 9),
                ("fasttech", 9), ("gearbest", 9), ("pag seguro", 9)
            ]),

            new KeywordCategoryRule("Terceiros",
            [
                ("terceiros", 10), ("pagamento terceiros", 10)
            ]),

            new KeywordCategoryRule("Alimentação",
            [
                ("55.769.239 ana paula r", 11), ("jw lima", 11),
                ("pao de acucar", 11), ("pão de açúcar", 11),
                ("divino fogao", 11), ("divino fogão", 11),
                ("restaurante", 10), ("restaur", 10), ("pizzaria", 10),
                ("hamburgueria", 10), ("churrascaria", 10),
                ("cantina", 9), ("boteco", 9), ("bar", 8), ("pastel", 10),
                ("keeta", 10), ("ragazzo", 10), ("salgados", 10), ("thoca", 10),
                ("mcdonalds", 10), ("mcdonaldsecommerce", 11), ("99food", 11),
                ("barueri drive", 10), ("emporio", 9),
                ("uber eats", 10), ("ifood", 10), ("ifd", 9),
                ("padaria", 10), ("açougue", 10),
                ("café", 9), ("cafeteria", 9), ("lanchonete", 9),
                ("sorveteria", 9), ("açaí", 9), ("suco", 8), ("smoothie", 8)
            ]),

            new KeywordCategoryRule("Supermercado",
            [
                ("supermercado", 11), ("supermercados", 11), ("mercado", 9),
                ("hortifruti", 9), ("feira", 8),
                ("carrefour", 10), ("atacadao", 10), ("atacadão", 10),
                ("assai", 10), ("assaí", 10), ("quasetudo", 10),
                ("nescafe", 10), ("center car", 10)
            ]),

            new KeywordCategoryRule("Transporte",
            [
                ("uber", 10), ("uberrides", 10), ("99", 10), ("99pop", 10),
                ("taxi", 10), ("táxi", 10), ("bolt", 10), ("loggi", 9),
                ("passagem", 9), ("ônibus", 9), ("metrô", 9), ("trem", 9),
                ("picpay", 9),
                ("combustível", 10), ("gasolina", 10), ("diesel", 10), ("etanol", 10),
                ("shell", 9), ("esso", 9), ("br", 8), ("ipiranga", 9),
                ("estacionamento", 10), ("garagem", 9), ("manobrista", 9),
                ("oficina", 10), ("mecânica", 10), ("mecanica", 10),
                ("pneu", 9), ("bateria", 9), ("óleo", 9), ("oleo", 9),
                ("filtro", 9), ("reparo", 9), ("manutenção", 9), ("manutencao", 9)
            ]),

            new KeywordCategoryRule("Saúde",
            [
                ("rdsaude", 11), ("farmácia", 10), ("farmacia", 10),
                ("manipulação", 10), ("drogaria", 10), ("hospital", 10),
                ("clínica", 10), ("clinica", 10), ("médico", 10), ("medico", 10),
                ("consultório", 10), ("consultorio", 10), ("odontológo", 10),
                ("dentista", 10), ("oftalmologista", 10),
                ("psicólogo", 10), ("psicologo", 10),
                ("vacina", 9), ("exame", 9), ("ressonância", 9), ("tomografia", 9)
            ]),

            new KeywordCategoryRule("Educação",
            [
                ("udemy", 10), ("coursera", 10), ("escola", 10),
                ("universidade", 10), ("faculdade", 10), ("curso", 9),
                ("professor", 9), ("aula", 8), ("reforço", 9),
                ("tutoria", 9), ("lingvo", 9), ("duolingo", 9)
            ]),

            new KeywordCategoryRule("Utilidades",
            [
                ("energia", 10), ("água", 10), ("internet", 10),
                ("telefone", 10), ("celular", 10),
                ("nubank", 9), ("itau", 9), ("bradesco", 9), ("caixa", 9), ("santander", 9),
                ("conta", 8), ("nuvem", 9), ("hospedagem", 9), ("dominio", 9),
                ("vpn", 9), ("seguro", 9),
                ("banco", 9), ("transferência", 9), ("boleto", 9), ("pix", 9)
            ]),

            new KeywordCategoryRule("Casa",
            [
                ("imobiliaria", 10), ("imobiliária", 10), ("aluguel", 10),
                ("condominio", 10), ("condomínio", 10),
                ("construção", 10), ("construcao", 10), ("reforma", 9),
                ("pintura", 9), ("carpintaria", 9), ("encanamento", 9),
                ("eletricista", 9), ("limpeza", 8), ("faxina", 8), ("material", 8)
            ]),

            new KeywordCategoryRule("Vestuário",
            [
                ("loja de roupas", 10), ("loja", 7), ("moda", 9), ("roupa", 9),
                ("sapato", 9), ("calçado", 9), ("bolsa", 9), ("cinto", 8),
                ("acessório", 8), ("acessorio", 8)
            ]),

            new KeywordCategoryRule("Diversão",
            [
                ("cinema", 10), ("ingresso", 10), ("teatro", 10), ("show", 10),
                ("parque", 9), ("museu", 9), ("diversão", 10), ("diversao", 10),
                ("jogo", 9), ("game", 9), ("console", 9),
                ("playstation", 9), ("xbox", 9), ("nintendo", 9)
            ]),

            new KeywordCategoryRule("Beleza",
            [
                ("beleza", 10), ("salão", 10), ("salon", 10), ("cabelo", 10),
                ("cabelereiro", 10), ("cabeleireiro", 10), ("barbaria", 10),
                ("barbearia", 10), ("barba", 9), ("manicure", 10), ("pedicure", 10),
                ("depilação", 10), ("depilacao", 10), ("cosmetologia", 10),
                ("estética", 10), ("estetica", 10), ("maquiagem", 10),
                ("maquiage", 10), ("perfume", 9), ("cosmetico", 9), ("cosmético", 9)
            ]),

            new KeywordCategoryRule("Esportes",
            [
                ("academia", 10), ("musculação", 10), ("musculacao", 10),
                ("pilates", 10), ("yoga", 10), ("esporte", 10), ("desporto", 10),
                ("natação", 10), ("natacao", 10), ("futebol", 10),
                ("tênis", 10), ("tenis", 10), ("dança", 10), ("danca", 10),
                ("lutas", 10), ("mma", 10), ("treino", 10)
            ])
        ];
    }
}