namespace FormaAI.Domain.Progress;

public enum ScheduleDecision { Moved, Skipped, Completed }
public enum ProgressPhotoPose { Front, Side, Back }
public enum ProgressionDecision { Pending, Accepted, Changed, Rejected, Used }

public sealed class WeeklyReview
{
    private WeeklyReview() { }

    public WeeklyReview(string userId, DateOnly weekStarting, int energy, int sleep, int hunger, int recovery, int stress, string? notes, string? deviationReasons)
    {
        Id = Guid.NewGuid(); UserId = userId; WeekStarting = weekStarting; Energy = energy; Sleep = sleep;
        Hunger = hunger; Recovery = recovery; Stress = stress; Notes = Clean(notes); DeviationReasons = Clean(deviationReasons);
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = null!;
    public DateOnly WeekStarting { get; private set; }
    public int Energy { get; private set; }
    public int Sleep { get; private set; }
    public int Hunger { get; private set; }
    public int Recovery { get; private set; }
    public int Stress { get; private set; }
    public string? Notes { get; private set; }
    public string? DeviationReasons { get; private set; }
    public string? Summary { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public void SetSummary(string summary) => Summary = summary.Trim();
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class ProgressPhoto
{
    private ProgressPhoto() { }

    public ProgressPhoto(string userId, DateOnly localDate, ProgressPhotoPose pose, string storageName, string contentType)
    {
        Id = Guid.NewGuid(); UserId = userId; LocalDate = localDate; Pose = pose; StorageName = storageName;
        ContentType = contentType; CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = null!;
    public DateOnly LocalDate { get; private set; }
    public ProgressPhotoPose Pose { get; private set; }
    public string StorageName { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }
}

public sealed class TrainingScheduleException
{
    private TrainingScheduleException() { }

    public TrainingScheduleException(string userId, Guid trainingDayId, DateOnly originalDate, DateOnly? newDate, ScheduleDecision decision, string? reason)
    {
        Id = Guid.NewGuid(); UserId = userId; TrainingDayId = trainingDayId; OriginalDate = originalDate;
        NewDate = newDate; Decision = decision; Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(); CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = null!;
    public Guid TrainingDayId { get; private set; }
    public DateOnly OriginalDate { get; private set; }
    public DateOnly? NewDate { get; private set; }
    public ScheduleDecision Decision { get; private set; }
    public string? Reason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}

public sealed class ExerciseProgression
{
    private ExerciseProgression() { }

    public ExerciseProgression(string userId, Guid exerciseId, Guid sourceSessionId, decimal weightKg, int minReps, int maxReps, string reason)
    {
        Id = Guid.NewGuid(); UserId = userId; ExerciseId = exerciseId; SourceSessionId = sourceSessionId;
        SuggestedWeightKg = weightKg; MinReps = minReps; MaxReps = maxReps; Reason = reason;
        Decision = ProgressionDecision.Pending; CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = null!;
    public Guid ExerciseId { get; private set; }
    public Guid SourceSessionId { get; private set; }
    public decimal SuggestedWeightKg { get; private set; }
    public int MinReps { get; private set; }
    public int MaxReps { get; private set; }
    public string Reason { get; private set; } = null!;
    public ProgressionDecision Decision { get; private set; }
    public decimal? AcceptedWeightKg { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UsedAtUtc { get; private set; }

    public void Decide(ProgressionDecision decision, decimal? weightKg = null)
    {
        Decision = decision; AcceptedWeightKg = weightKg;
    }

    public void MarkUsed() { Decision = ProgressionDecision.Used; UsedAtUtc = DateTime.UtcNow; }
}

public sealed class PushSubscription
{
    private PushSubscription() { }

    public PushSubscription(string userId, string endpoint, string p256Dh, string auth)
    {
        Id = Guid.NewGuid(); UserId = userId; Endpoint = endpoint; P256Dh = p256Dh; Auth = auth; CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = null!;
    public string Endpoint { get; private set; } = null!;
    public string P256Dh { get; private set; } = null!;
    public string Auth { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }
}

public sealed class NotificationDelivery
{
    private NotificationDelivery() { }

    public NotificationDelivery(string userId, DateOnly localDate, string eventKey)
    {
        Id = Guid.NewGuid(); UserId = userId; LocalDate = localDate; EventKey = eventKey; SentAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = null!;
    public DateOnly LocalDate { get; private set; }
    public string EventKey { get; private set; } = null!;
    public DateTime SentAtUtc { get; private set; }
}
