using FormaAI.Application.Assistant;

namespace FormaAI.Application.Tests;

public sealed class AssistantFailureCopyTests
{
    [Fact]
    public void WorkoutDraftFailureUsesTrainingLanguage()
    {
        var message = AssistantFailureCopy.ForRequest(
            "Użyj create_completed_workout_draft i przygotuj trening z opisu użytkownika.");

        Assert.Contains("ćwiczenia", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("serie", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("produkt", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("porcj", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MealFailureKeepsNutritionRecovery()
    {
        var message = AssistantFailureCopy.ForRequest("Dodaj posiłek ze zdjęcia.");

        Assert.Contains("produkt", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("porcj", message, StringComparison.OrdinalIgnoreCase);
    }
}
