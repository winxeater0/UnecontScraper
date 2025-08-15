namespace UneCont.Scraper.Utilities;

public static class RatingParser
{
    public static int FromCssClass(string? classString)
    {
        if (string.IsNullOrWhiteSpace(classString)) return 0;
        if (classString.Contains("One", StringComparison.OrdinalIgnoreCase)) return 1;
        if (classString.Contains("Two", StringComparison.OrdinalIgnoreCase)) return 2;
        if (classString.Contains("Three", StringComparison.OrdinalIgnoreCase)) return 3;
        if (classString.Contains("Four", StringComparison.OrdinalIgnoreCase)) return 4;
        if (classString.Contains("Five", StringComparison.OrdinalIgnoreCase)) return 5;
        return 0;
    }
}