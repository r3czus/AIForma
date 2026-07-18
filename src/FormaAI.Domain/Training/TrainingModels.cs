namespace FormaAI.Domain.Training;

public enum MuscleGroup { Chest, Back, Shoulders, Biceps, Triceps, Quadriceps, Hamstrings, Glutes, Calves, Core, FullBody }
public enum Equipment { Barbell, Dumbbell, Machine, Cable, Bodyweight, Kettlebell, Other }
public enum PlanSource { Manual, AssistantDraft }
public enum SessionStatus { InProgress, Completed, Abandoned }
public enum SetType { Warmup, Working }

public sealed class Exercise
{
    private Exercise() { }
    public Exercise(string? ownerUserId, string name, MuscleGroup muscleGroup, Equipment equipment, bool unilateral = false, string? description = null)
    {
        Id = Guid.NewGuid(); OwnerUserId = ownerUserId; Name = name.Trim(); PrimaryMuscleGroup = muscleGroup;
        Equipment = equipment; IsUnilateral = unilateral; Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsActive = true; CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string? OwnerUserId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public MuscleGroup PrimaryMuscleGroup { get; private set; }
    public Equipment Equipment { get; private set; }
    public bool IsUnilateral { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public void Update(string name, MuscleGroup muscleGroup, Equipment equipment, bool unilateral, string? description)
    {
        Name = name.Trim(); PrimaryMuscleGroup = muscleGroup; Equipment = equipment; IsUnilateral = unilateral;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(); UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class TrainingPlan
{
    private TrainingPlan() { }
    public TrainingPlan(string userId, string name, string goal, DateOnly startsOn)
    {
        Id = Guid.NewGuid(); UserId = userId; Name = name.Trim(); Goal = goal.Trim(); StartsOn = startsOn;
        Source = PlanSource.Manual; CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Goal { get; private set; } = null!;
    public bool IsActive { get; set; }
    public DateOnly StartsOn { get; private set; }
    public PlanSource Source { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public List<TrainingDay> Days { get; private set; } = [];

    public void Replace(string name, string goal, DateOnly startsOn, IEnumerable<TrainingDay> days)
    {
        Name = name.Trim(); Goal = goal.Trim(); StartsOn = startsOn; Days.Clear(); Days.AddRange(days); UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsAssistantDraft() => Source = PlanSource.AssistantDraft;
}

public sealed class TrainingDay
{
    private TrainingDay() { }
    public TrainingDay(string name, DayOfWeek? dayOfWeek, int sequenceNumber)
    {
        Id = Guid.NewGuid(); Name = name.Trim(); DayOfWeek = dayOfWeek; SequenceNumber = sequenceNumber;
    }
    public Guid Id { get; private set; }
    public Guid TrainingPlanId { get; private set; }
    public string Name { get; private set; } = null!;
    public DayOfWeek? DayOfWeek { get; private set; }
    public int SequenceNumber { get; private set; }
    public List<PlannedExercise> Exercises { get; private set; } = [];
}

public sealed class PlannedExercise
{
    private PlannedExercise() { }
    public PlannedExercise(Guid exerciseId, int order, int sets, int minReps, int maxReps, decimal? targetRir, int? restSeconds)
    {
        Id = Guid.NewGuid(); ExerciseId = exerciseId; Order = order; Sets = sets; MinReps = minReps;
        MaxReps = maxReps; TargetRir = targetRir; RestSeconds = restSeconds;
    }
    public Guid Id { get; private set; }
    public Guid TrainingDayId { get; private set; }
    public Guid ExerciseId { get; private set; }
    public int Order { get; private set; }
    public int Sets { get; private set; }
    public int MinReps { get; private set; }
    public int MaxReps { get; private set; }
    public decimal? TargetRir { get; private set; }
    public int? RestSeconds { get; private set; }
    public string? Notes { get; private set; }
}

public sealed class WorkoutSession
{
    private WorkoutSession() { }
    public WorkoutSession(string userId, TrainingPlan plan, TrainingDay day)
    {
        Id = Guid.NewGuid(); UserId = userId; TrainingPlanId = plan.Id; TrainingDayId = day.Id;
        NameSnapshot = day.Name; StartedAtUtc = DateTime.UtcNow; Status = SessionStatus.InProgress;
    }
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = null!;
    public Guid? TrainingPlanId { get; private set; }
    public Guid? TrainingDayId { get; private set; }
    public string NameSnapshot { get; private set; } = null!;
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? FinishedAtUtc { get; private set; }
    public SessionStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public List<WorkoutExercise> Exercises { get; private set; } = [];
    public void Finish(SessionStatus status) { Status = status; FinishedAtUtc = DateTime.UtcNow; }
}

public sealed class WorkoutExercise
{
    private WorkoutExercise() { }
    public WorkoutExercise(PlannedExercise planned, Exercise exercise)
    {
        Id = Guid.NewGuid(); ExerciseId = exercise.Id; ExerciseNameSnapshot = exercise.Name; Order = planned.Order;
        PlannedSets = planned.Sets; MinReps = planned.MinReps; MaxReps = planned.MaxReps;
        TargetRir = planned.TargetRir; RestSeconds = planned.RestSeconds;
    }
    public Guid Id { get; private set; }
    public Guid WorkoutSessionId { get; private set; }
    public Guid? ExerciseId { get; private set; }
    public string ExerciseNameSnapshot { get; private set; } = null!;
    public int Order { get; private set; }
    public int PlannedSets { get; private set; }
    public int MinReps { get; private set; }
    public int MaxReps { get; private set; }
    public decimal? TargetRir { get; private set; }
    public int? RestSeconds { get; private set; }
    public List<CompletedSet> Sets { get; private set; } = [];
}

public sealed class CompletedSet
{
    private CompletedSet() { }
    public CompletedSet(Guid workoutExerciseId, int setNumber, decimal weightKg, int repetitions, decimal? rir, SetType type)
    {
        Id = Guid.NewGuid(); WorkoutExerciseId = workoutExerciseId; SetNumber = setNumber; WeightKg = weightKg; Repetitions = repetitions;
        Rir = rir; Type = type; CompletedAtUtc = DateTime.UtcNow;
    }
    public Guid Id { get; private set; }
    public Guid WorkoutExerciseId { get; private set; }
    public int SetNumber { get; private set; }
    public decimal WeightKg { get; private set; }
    public int Repetitions { get; private set; }
    public decimal? Rir { get; private set; }
    public SetType Type { get; private set; }
    public DateTime CompletedAtUtc { get; private set; }
    public string? Notes { get; private set; }
    public void Update(decimal weightKg, int repetitions, decimal? rir, SetType type) { WeightKg = weightKg; Repetitions = repetitions; Rir = rir; Type = type; }
}
