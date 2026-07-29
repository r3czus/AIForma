using FormaAI.Application.Training;

namespace FormaAI.Application.Tests;

public sealed class WorkoutSetSeedDecisionTests
{
    private static readonly WorkoutSetSeedSnapshot Defaults = new(0m, 8, 2m);

    [Fact]
    public void DelayedHistoryDoesNotSeedAfterRawInteractionWhenParsedValuesStillMatch()
    {
        var canSeed = WorkoutSetSeedDecision.CanApplyDelayedHistory(
            seedRequested: true,
            userInteracted: true,
            completedSetCount: 0,
            captured: Defaults,
            current: Defaults);

        Assert.False(canSeed);
    }

    [Fact]
    public void DelayedHistorySeedsUntouchedDefaultForm()
    {
        var canSeed = WorkoutSetSeedDecision.CanApplyDelayedHistory(
            seedRequested: true,
            userInteracted: false,
            completedSetCount: 0,
            captured: Defaults,
            current: Defaults);

        Assert.True(canSeed);
    }

    [Fact]
    public void DelayedHistoryDoesNotSeedAfterParsedValuesChange()
    {
        var canSeed = WorkoutSetSeedDecision.CanApplyDelayedHistory(
            seedRequested: true,
            userInteracted: false,
            completedSetCount: 0,
            captured: Defaults,
            current: Defaults with { WeightKg = 42.5m });

        Assert.False(canSeed);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void ProgrammaticPresetSeedingStopsAfterInteraction(bool userInteracted, bool expected)
    {
        Assert.Equal(expected, WorkoutSetSeedDecision.CanApplyProgrammaticSeed(userInteracted));
    }
}
