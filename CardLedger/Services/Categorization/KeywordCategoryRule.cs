using System.Text.RegularExpressions;

namespace CardLedger.Services.Categorization;

public sealed class KeywordCategoryRule(
    string category,
    IReadOnlyList<(string Keyword, int Priority)> keywords) : ICategoryRule
{
    private const int FuzzyThreshold = 70;

    public CategoryMatch? Match(string title)
    {
        var best = ExactBestPriority(title);

        if (best == 0)
            best = FuzzyBestPriority(title);

        return best > 0 ? new CategoryMatch(category, best) : null;
    }

    private int ExactBestPriority(string title)
    {
        int best = 0;
        foreach (var (keyword, priority) in keywords)
        {
            if (IsMatch(title, keyword))
                best = Math.Max(best, priority);
        }
        return best;
    }

    private int FuzzyBestPriority(string title)
    {
        var words = title.Split([' ', '-', '/', '.', '*'], StringSplitOptions.RemoveEmptyEntries);
        int best = 0;

        foreach (var word in words)
        {
            foreach (var (keyword, priority) in keywords)
            {
                if (!keyword.Contains(' ') && Similarity(word, keyword) >= FuzzyThreshold)
                    best = Math.Max(best, priority);
            }
        }

        return best;
    }

    private static bool IsMatch(string title, string keyword) =>
        keyword.Contains(' ')
            ? title.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            : Regex.IsMatch(title,
                $@"(?<!\w){Regex.Escape(keyword)}(?!\w)",
                RegexOptions.IgnoreCase);

    private static int Similarity(string a, string b)
    {
        a = a.ToLower();
        b = b.ToLower();
        if (a == b) return 100;
        int max = Math.Max(a.Length, b.Length);
        return max == 0 ? 100 : (int)((1.0 - (double)LevenshteinDistance(a, b) / max) * 100);
    }

    private static int LevenshteinDistance(string s, string t)
    {
        int sLen = s.Length, tLen = t.Length;
        var d = new int[sLen + 1, tLen + 1];
        for (int i = 0; i <= sLen; i++) d[i, 0] = i;
        for (int j = 0; j <= tLen; j++) d[0, j] = j;
        for (int i = 1; i <= sLen; i++)
            for (int j = 1; j <= tLen; j++)
            {
                int cost = s[i - 1] == t[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        return d[sLen, tLen];
    }
}
