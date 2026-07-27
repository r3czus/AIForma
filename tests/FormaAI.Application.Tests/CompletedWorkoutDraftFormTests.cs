using FormaAI.Application.Assistant;
using FormaAI.Contracts.Assistant;
using FormaAI.Domain.Assistant;

namespace FormaAI.Application.Tests;

public sealed class CompletedWorkoutDraftFormTests
{
    [Fact]
    public void FormMapsEditableDraftBackToUpdateRequest()
    {
        var exerciseId = Guid.NewGuid();
        var response = new AssistantCompletedWorkoutDraftResponse(
            Guid.NewGuid(),
            AssistantDraftStatus.Pending,
            new DateOnly(2026, 7, 27),
            "Trening z AI",
            [
                new AssistantWorkoutExerciseDraft(
                    exerciseId,
                    "Wyciskanie",
                    [new AssistantWorkoutSetDraft(80, 8, 2)])
            ],
            DateTime.UtcNow.AddMinutes(30));

        var form = CompletedWorkoutDraftForm.From(response);
        form.Exercises[0].Sets[0].WeightKg = 82.5m;
        var request = form.ToRequest();

        Assert.Empty(form.Validate());
        Assert.Equal(response.Id, form.Id);
        Assert.Equal(82.5m, request.Exercises.Single().Sets.Single().WeightKg);
    }

    [Fact]
    public void FormRejectsExerciseWithoutSets()
    {
        var form = new CompletedWorkoutDraftForm
        {
            Id = Guid.NewGuid(),
            LocalDate = DateOnly.FromDateTime(DateTime.Today),
            Name = "Niepełny",
            Exercises =
            [
                new CompletedWorkoutExerciseForm
                {
                    ExerciseId = Guid.NewGuid(),
                    ExerciseName = "Przysiad"
                }
            ]
        };

        Assert.Contains(form.Validate(), error => error.Contains("serię", StringComparison.OrdinalIgnoreCase));
        Assert.Throws<InvalidOperationException>(() => form.ToRequest());
    }

    [Fact]
    public void FormPreservesCardioFromAiDraft()
    {
        var response = new AssistantCompletedWorkoutDraftResponse(
            Guid.NewGuid(),
            AssistantDraftStatus.Pending,
            new DateOnly(2026, 7, 28),
            "Bieg i siła",
            [],
            DateTime.UtcNow.AddMinutes(30),
            [new AssistantWorkoutCardioDraft("Bieg na bieżni", 40, 5, null)]);

        var form = CompletedWorkoutDraftForm.From(response);
        var request = form.ToRequest();

        Assert.Empty(form.Validate());
        Assert.Equal(40, Assert.Single(request.Cardio!).DurationMinutes);
        Assert.Empty(request.Exercises);
    }
}
