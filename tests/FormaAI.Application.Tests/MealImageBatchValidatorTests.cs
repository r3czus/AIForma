using FormaAI.Application.Nutrition;

namespace FormaAI.Application.Tests;

public sealed class MealImageBatchValidatorTests
{
    [Fact]
    public void AcceptsOneToFiveSupportedImages()
    {
        var images = Enumerable.Range(0, 5)
            .Select(_ => new MealImageDescriptor("image/jpeg", 1024L))
            .ToList();

        Assert.Null(MealImageBatchValidator.Validate(images));
    }

    [Fact]
    public void RejectsEmptyBatch() =>
        Assert.Equal("Wybierz od 1 do 5 zdjęć.", MealImageBatchValidator.Validate([]));

    [Fact]
    public void RejectsMoreThanFiveImages() =>
        Assert.Equal("Wybierz od 1 do 5 zdjęć.", MealImageBatchValidator.Validate(
            Enumerable.Repeat(new MealImageDescriptor("image/png", 1L), 6).ToList()));

    [Theory]
    [InlineData("image/gif", 100)]
    [InlineData("text/plain", 100)]
    [InlineData("image/jpeg", 12582913)]
    [InlineData("image/jpeg", 0)]
    public void RejectsUnsupportedOrInvalidImage(string contentType, long length) =>
        Assert.NotNull(MealImageBatchValidator.Validate([new(contentType, length)]));
}
