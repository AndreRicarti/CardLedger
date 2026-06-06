using System.Text.RegularExpressions;

namespace CardLedger.Services.Categorization;

public sealed class RegexCategoryRule(string pattern, string category, int priority = int.MaxValue) : ICategoryRule
{
    private readonly Regex _regex = new(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public CategoryMatch? Match(string title) =>
        _regex.IsMatch(title) ? new CategoryMatch(category, priority) : null;
}
