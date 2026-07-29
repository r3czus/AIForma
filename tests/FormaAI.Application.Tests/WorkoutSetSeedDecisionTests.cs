using FormaAI.Application.Training;

namespace FormaAI.Application.Tests;

public sealed class WorkoutSetSeedDecisionTests
{
    private static readonly WorkoutSetSeedSnapshot Defaults = new(0m, 8, 2m);
    private static readonly WorkoutSetSeedLifecycle Untouched = new(0, false);

    [Fact]
    public void DelayedHistoryDoesNotSeedAfterRawInteractionWhenParsedValuesStillMatch()
    {
        var canSeed = WorkoutSetSeedDecision.CanApplyDelayedHistory(
            seedRequested: true,
            capturedLifecycle: Untouched,
            currentLifecycle: Untouched.MarkInteracted(),
            capturedCompletedSetCount: 0,
            currentCompletedSetCount: 0,
            captured: Defaults,
            current: Defaults);

        Assert.False(canSeed);
    }

    [Fact]
    public void DelayedHistorySeedsUntouchedDefaultForm()
    {
        var canSeed = WorkoutSetSeedDecision.CanApplyDelayedHistory(
            seedRequested: true,
            capturedLifecycle: Untouched,
            currentLifecycle: Untouched,
            capturedCompletedSetCount: 0,
            currentCompletedSetCount: 0,
            captured: Defaults,
            current: Defaults);

        Assert.True(canSeed);
    }

    [Fact]
    public void DelayedHistoryDoesNotSeedAfterParsedValuesChange()
    {
        var canSeed = WorkoutSetSeedDecision.CanApplyDelayedHistory(
            seedRequested: true,
            capturedLifecycle: Untouched,
            currentLifecycle: Untouched,
            capturedCompletedSetCount: 0,
            currentCompletedSetCount: 0,
            captured: Defaults,
            current: Defaults with { WeightKg = 42.5m });

        Assert.False(canSeed);
    }

    [Fact]
    public void AdjustedFirstSetAdvancesToCleanLifecycleAndAllowsSecondPreset()
    {
        var firstSet = Untouched.MarkInteracted();

        Assert.False(WorkoutSetSeedDecision.CanApplyProgrammaticSeed(firstSet));
        Assert.Null(WorkoutSetSeedDecision.NextPresetSetNumber(1, firstSet));

        var secondSet = firstSet.Advance();

        Assert.Equal(firstSet.Generation + 1, secondSet.Generation);
        Assert.False(secondSet.UserInteracted);
        Assert.True(WorkoutSetSeedDecision.CanApplyProgrammaticSeed(secondSet));
        Assert.Equal(2, WorkoutSetSeedDecision.NextPresetSetNumber(1, secondSet));
    }

    [Fact]
    public void DelayedHistoryFromFirstSetCannotOverwriteSecondSetAfterGenerationAdvances()
    {
        var secondSet = Untouched.MarkInteracted().Advance();

        var canSeedWithSameCount = WorkoutSetSeedDecision.CanApplyDelayedHistory(
            seedRequested: true,
            capturedLifecycle: Untouched,
            currentLifecycle: secondSet,
            capturedCompletedSetCount: 0,
            currentCompletedSetCount: 0,
            captured: Defaults,
            current: Defaults);
        var canSeedWithRefreshedCount = WorkoutSetSeedDecision.CanApplyDelayedHistory(
            seedRequested: true,
            capturedLifecycle: Untouched,
            currentLifecycle: secondSet,
            capturedCompletedSetCount: 0,
            currentCompletedSetCount: 1,
            captured: Defaults,
            current: Defaults);

        Assert.False(canSeedWithSameCount);
        Assert.False(canSeedWithRefreshedCount);
    }

    [Fact]
    public void DelayedHistoryUsesRefreshedCompletedSetCount()
    {
        var canSeed = WorkoutSetSeedDecision.CanApplyDelayedHistory(
            seedRequested: true,
            capturedLifecycle: Untouched,
            currentLifecycle: Untouched,
            capturedCompletedSetCount: 0,
            currentCompletedSetCount: 1,
            captured: Defaults,
            current: Defaults);

        Assert.False(canSeed);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void ProgrammaticPresetSeedingStopsAfterInteraction(bool userInteracted, bool expected)
    {
        var lifecycle = new WorkoutSetSeedLifecycle(0, userInteracted);

        Assert.Equal(expected, WorkoutSetSeedDecision.CanApplyProgrammaticSeed(lifecycle));
    }
}
