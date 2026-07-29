namespace FormaAI.Application.Training;

public readonly record struct WorkoutSetSeedSnapshot(
    decimal WeightKg,
    int Repetitions,
    decimal? Rir);

public readonly record struct WorkoutSetSeedLifecycle(
    int Generation,
    bool UserInteracted)
{
    public WorkoutSetSeedLifecycle MarkInteracted() => this with { UserInteracted = true };

    public WorkoutSetSeedLifecycle Advance() => new(checked(Generation + 1), false);
}

public static class WorkoutSetSeedDecision
{
    public static bool CanApplyDelayedHistory(
        bool seedRequested,
        WorkoutSetSeedLifecycle capturedLifecycle,
        WorkoutSetSeedLifecycle currentLifecycle,
        int capturedCompletedSetCount,
        int currentCompletedSetCount,
        WorkoutSetSeedSnapshot captured,
        WorkoutSetSeedSnapshot current)
    {
        return seedRequested &&
               !currentLifecycle.UserInteracted &&
               currentLifecycle.Generation == capturedLifecycle.Generation &&
               capturedCompletedSetCount == 0 &&
               currentCompletedSetCount == capturedCompletedSetCount &&
               captured == current;
    }

    public static bool CanApplyProgrammaticSeed(WorkoutSetSeedLifecycle lifecycle)
    {
        return !lifecycle.UserInteracted;
    }

    public static int? NextPresetSetNumber(
        int completedWorkingSetCount,
        WorkoutSetSeedLifecycle lifecycle)
    {
        return CanApplyProgrammaticSeed(lifecycle)
            ? checked(completedWorkingSetCount + 1)
            : null;
    }
}
