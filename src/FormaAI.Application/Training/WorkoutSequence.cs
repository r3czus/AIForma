namespace FormaAI.Application.Training;

public enum WorkoutTimerKind
{
    None,
    Interval,
    Rest
}

public sealed record WorkoutExerciseState(
    Guid Id,
    int Order,
    int PlannedSets,
    int CompletedSets,
    Guid? SupersetGroupId,
    int? SupersetPosition,
    int? IntervalSeconds,
    int? RestSeconds);

public sealed record WorkoutStep(Guid? NextExerciseId, WorkoutTimerKind Timer, int Seconds);

public static class WorkoutSequence
{
    public static WorkoutStep Next(IReadOnlyList<WorkoutExerciseState> exercises, Guid completedExerciseId)
    {
        var ordered = exercises.OrderBy(x => x.Order).ToList();
        var current = ordered.SingleOrDefault(x => x.Id == completedExerciseId)
            ?? throw new ArgumentException("Ćwiczenie nie należy do sesji.", nameof(completedExerciseId));

        if (current.SupersetGroupId is not Guid groupId)
            return NextStandalone(ordered, current);

        var group = ordered
            .Where(x => x.SupersetGroupId == groupId)
            .OrderBy(x => x.SupersetPosition)
            .ThenBy(x => x.Order)
            .ToList();
        var currentIndex = group.FindIndex(x => x.Id == current.Id);
        var nextInRound = group
            .Skip(currentIndex + 1)
            .FirstOrDefault(x => x.CompletedSets < current.CompletedSets && x.CompletedSets < x.PlannedSets);

        if (nextInRound is not null)
            return new WorkoutStep(nextInRound.Id, WorkoutTimerKind.Interval, current.IntervalSeconds ?? 0);

        var nextRoundMember = group
            .Where(x => x.CompletedSets < x.PlannedSets)
            .OrderBy(x => x.CompletedSets)
            .ThenBy(x => x.SupersetPosition)
            .FirstOrDefault();
        if (nextRoundMember is not null)
            return new WorkoutStep(nextRoundMember.Id, WorkoutTimerKind.Rest, current.RestSeconds ?? 0);

        var groupLastOrder = group.Max(x => x.Order);
        var nextExercise = ordered.FirstOrDefault(x => x.Order > groupLastOrder && x.CompletedSets < x.PlannedSets);
        return nextExercise is null
            ? new WorkoutStep(null, WorkoutTimerKind.None, 0)
            : new WorkoutStep(nextExercise.Id, WorkoutTimerKind.Rest, current.RestSeconds ?? 0);
    }

    private static WorkoutStep NextStandalone(IReadOnlyList<WorkoutExerciseState> ordered, WorkoutExerciseState current)
    {
        if (current.CompletedSets < current.PlannedSets)
            return new WorkoutStep(current.Id, WorkoutTimerKind.Rest, current.RestSeconds ?? 0);

        var next = ordered.FirstOrDefault(x => x.Order > current.Order && x.CompletedSets < x.PlannedSets);
        return next is null
            ? new WorkoutStep(null, WorkoutTimerKind.None, 0)
            : new WorkoutStep(next.Id, WorkoutTimerKind.Rest, current.RestSeconds ?? 0);
    }
}
