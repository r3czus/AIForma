namespace FormaAI.Domain.Users;

public sealed class UserProfile
{
    private UserProfile() { }

    public UserProfile(string userId, string timeZoneId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        TimeZoneId = timeZoneId;
        CalorieToleranceKcal = 100;
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = null!;
    public string TimeZoneId { get; private set; } = null!;
    public decimal? HeightCm { get; private set; }
    public decimal? StartingWeightKg { get; private set; }
    public BodyGoal? Goal { get; private set; }
    public int? PlannedTrainingsPerWeek { get; private set; }
    public DateOnly? BirthDate { get; private set; }
    public BiologicalSex? Sex { get; private set; }
    public ActivityLevel? ActivityLevel { get; private set; }
    public decimal? TargetWeightKg { get; private set; }
    public int CalorieToleranceKcal { get; private set; }

    public void Update(
        string timeZoneId,
        decimal? heightCm,
        decimal? startingWeightKg,
        BodyGoal? goal,
        int? plannedTrainingsPerWeek,
        DateOnly? birthDate = null,
        BiologicalSex? sex = null,
        ActivityLevel? activityLevel = null,
        decimal? targetWeightKg = null,
        int calorieToleranceKcal = 100)
    {
        TimeZoneId = timeZoneId;
        HeightCm = heightCm;
        StartingWeightKg = startingWeightKg;
        Goal = goal;
        PlannedTrainingsPerWeek = plannedTrainingsPerWeek;
        BirthDate = birthDate;
        Sex = sex;
        ActivityLevel = activityLevel;
        TargetWeightKg = targetWeightKg;
        CalorieToleranceKcal = calorieToleranceKcal;
    }
}
