namespace FormaAI.Domain.Users;

public sealed class UserProfile
{
    private UserProfile() { }

    public UserProfile(string userId, string timeZoneId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        TimeZoneId = timeZoneId;
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = null!;
    public string TimeZoneId { get; private set; } = null!;
    public decimal? HeightCm { get; private set; }
    public decimal? StartingWeightKg { get; private set; }
    public BodyGoal? Goal { get; private set; }
    public int? PlannedTrainingsPerWeek { get; private set; }

    public void Update(
        string timeZoneId,
        decimal? heightCm,
        decimal? startingWeightKg,
        BodyGoal? goal,
        int? plannedTrainingsPerWeek)
    {
        TimeZoneId = timeZoneId;
        HeightCm = heightCm;
        StartingWeightKg = startingWeightKg;
        Goal = goal;
        PlannedTrainingsPerWeek = plannedTrainingsPerWeek;
    }
}
