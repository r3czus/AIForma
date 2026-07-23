using FormaAI.Application.Common;

namespace FormaAI.Application.Tests;

public sealed class WildcardSearchTests
{
    [Theory]
    [InlineData("wycisk", "wycisk", WildcardSearchMode.Contains)]
    [InlineData("wycisk*", "wycisk", WildcardSearchMode.StartsWith)]
    [InlineData("*wycisk", "wycisk", WildcardSearchMode.EndsWith)]
    [InlineData("*wycisk*", "wycisk", WildcardSearchMode.Contains)]
    [InlineData("  *wycisk*  ", "wycisk", WildcardSearchMode.Contains)]
    [InlineData("*", "", WildcardSearchMode.Contains)]
    public void ParseRecognizesEdgeWildcards(string query, string value, WildcardSearchMode mode)
    {
        Assert.Equal(new WildcardSearchPattern(value, mode), WildcardSearch.Parse(query));
    }
}
