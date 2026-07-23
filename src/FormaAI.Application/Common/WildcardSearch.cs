namespace FormaAI.Application.Common;

public enum WildcardSearchMode
{
    Contains,
    StartsWith,
    EndsWith
}

public readonly record struct WildcardSearchPattern(string Value, WildcardSearchMode Mode);

public static class WildcardSearch
{
    public static WildcardSearchPattern Parse(string? query)
    {
        var value = query?.Trim() ?? string.Empty;
        var startsWithWildcard = value.StartsWith('*');
        var endsWithWildcard = value.EndsWith('*');
        value = value.Trim('*').Trim();

        var mode = (startsWithWildcard, endsWithWildcard) switch
        {
            (false, true) => WildcardSearchMode.StartsWith,
            (true, false) => WildcardSearchMode.EndsWith,
            _ => WildcardSearchMode.Contains
        };

        return new WildcardSearchPattern(value, mode);
    }
}
