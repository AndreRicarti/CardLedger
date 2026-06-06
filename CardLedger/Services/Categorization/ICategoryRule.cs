namespace CardLedger.Services.Categorization;

public readonly record struct CategoryMatch(string Category, int Priority);

public interface ICategoryRule
{
    CategoryMatch? Match(string title);
}
