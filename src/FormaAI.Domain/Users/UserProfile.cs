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
        MealSlots = "Śniadanie|Lunch|Obiad|Przekąska|Kolacja";
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
    public string MealSlots { get; private set; } = "Śniadanie|Lunch|Obiad|Przekąska|Kolacja";

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
        int calorieToleranceKcal = 100,
        IReadOnlyList<string>? mealSlots = null)
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
        if (mealSlots is not null)
        {
            var slots = mealSlots.Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Take(10).ToList();
            if (slots.Count > 0) MealSlots = string.Join('|', slots);
        }
    }
}
