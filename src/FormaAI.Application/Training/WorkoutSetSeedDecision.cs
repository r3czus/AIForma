namespace FormaAI.Application.Training;

public readonly record struct WorkoutSetSeedSnapshot(
    decimal WeightKg,
    int Repetitions,
    decimal? Rir);

public static class WorkoutSetSeedDecision
{
    public static bool CanApplyDelayedHistory(
        bool seedRequested,
        bool userInteracted,
        int completedSetCount,
        WorkoutSetSeedSnapshot captured,
        WorkoutSetSeedSnapshot current)
    {
        return seedRequested &&
               !userInteracted &&
               completedSetCount == 0 &&
               captured == current;
    }

    public static bool CanApplyProgrammaticSeed(bool userInteracted)
    {
        return !userInteracted;
    }
}
