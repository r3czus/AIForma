using FormaAI.Domain.Nutrition;

namespace FormaAI.Domain.Tests;

public sealed class MealCopyTests
{
    [Fact]
    public void Copy_creates_new_identifiers_and_preserves_snapshots()
    {
        var productId = Guid.NewGuid();
        var source = new Meal("user", "Śniadanie · Owsianka", DateTime.UtcNow, DateOnly.FromDateTime(DateTime.UtcNow));
        source.Items.Add(new MealItem(productId, "Płatki owsiane", 80, 300, 10, 6, 50, true));

        var operationId = Guid.NewGuid();
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var copy = source.CopyTo(DateTime.UtcNow.AddDays(1), targetDate, "Lunch", operationId);

        Assert.NotEqual(source.Id, copy.Id);
        Assert.Equal(operationId, copy.CopyOperationId);
        Assert.Equal(targetDate, copy.LocalDate);
        Assert.Equal("Lunch · Owsianka", copy.Name);
        Assert.Equal(source.Items.Single().AmountGrams, copy.Items.Single().AmountGrams);
        Assert.Equal(source.Items.Single().CaloriesKcal, copy.Items.Single().CaloriesKcal);
        Assert.Equal(source.Items.Single().IsEstimated, copy.Items.Single().IsEstimated);
        Assert.NotEqual(source.Items.Single().Id, copy.Items.Single().Id);
    }
}
