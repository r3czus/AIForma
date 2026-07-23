using FormaAI.Contracts.Assistant;

namespace FormaAI.Application.Tests;

public sealed class AiDefaultsTests
{
    [Fact]
    public void Gemini35FlashLiteIsTheDefaultModel()
    {
        Assert.Equal("gemini-3.5-flash-lite", AiDefaults.GeminiModel);
    }

    [Theory]
    [InlineData("gemini-3.5-flash-lite", "minimal")]
    [InlineData("GEMINI-3.5-FLASH-LITE", "minimal")]
    [InlineData("gemini-3.6-flash", "low")]
    public void ThinkingLevelMatchesModelFamily(string model, string expected)
    {
        Assert.Equal(expected, AiDefaults.GeminiThinkingLevel(model));
    }
}
