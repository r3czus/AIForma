using FormaAI.Contracts.Training;

namespace FormaAI.Application.Tests;

public sealed class ExerciseMediaPolicyTests
{
    [Theory]
    [InlineData("image/jpeg", "ruch.jpg", "image/jpeg", ".jpg")]
    [InlineData("image/jpeg", "ruch.jpeg", "image/jpeg", ".jpg")]
    [InlineData("image/png", "ruch.png", "image/png", ".png")]
    [InlineData("image/webp", "ruch.webp", "image/webp", ".webp")]
    [InlineData("image/gif", "ruch.gif", "image/gif", ".gif")]
    [InlineData("video/mp4", "ruch.mp4", "video/mp4", ".mp4")]
    [InlineData("video/webm", "ruch.webm", "video/webm", ".webm")]
    public void NormalizesAllowedMedia(
        string contentType,
        string fileName,
        string expectedType,
        string expectedExtension)
    {
        var accepted = ExerciseMediaPolicy.TryNormalize(
            contentType,
            fileName,
            out var normalizedType,
            out var extension);

        Assert.True(accepted);
        Assert.Equal(expectedType, normalizedType);
        Assert.Equal(expectedExtension, extension);
    }

    [Theory]
    [InlineData("image/svg+xml", "ruch.svg")]
    [InlineData("video/quicktime", "ruch.mov")]
    [InlineData("image/png", "ruch.exe")]
    [InlineData("video/mp4", "ruch.webm")]
    [InlineData("", "ruch.png")]
    [InlineData("image/png", "")]
    public void RejectsUnsupportedOrMismatchedMedia(string contentType, string fileName)
    {
        Assert.False(ExerciseMediaPolicy.TryNormalize(contentType, fileName, out _, out _));
    }
}
