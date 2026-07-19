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
        MealSchedule = "Śniadanie~07:00~1|Lunch~12:00~1|Obiad~15:00~1|Przekąska~18:00~1|Kolacja~21:00~1";
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
    public ActivityLevel? WorkActivityLevel { get; private set; }
    public ActivityLevel? TrainingActivityLevel { get; private set; }
    public decimal? TargetWeightKg { get; private set; }
    public int CalorieToleranceKcal { get; private set; }
    public string MealSlots { get; private set; } = "Śniadanie|Lunch|Obiad|Przekąska|Kolacja";
    public string MealSchedule { get; private set; } = "Śniadanie~07:00~1|Lunch~12:00~1|Obiad~15:00~1|Przekąska~18:00~1|Kolacja~21:00~1";
    public int DefaultRestSeconds { get; private set; } = 90;
    public int DefaultRepetitions { get; private set; } = 10;
    public int DefaultSets { get; private set; } = 3;
    public DayOfWeek WeekStartsOn { get; private set; } = DayOfWeek.Monday;
    public string ThemePreference { get; private set; } = "system";
    public bool MealRemindersEnabled { get; private set; }
    public int MealReminderMinutesBefore { get; private set; }
    public bool TrainingRemindersEnabled { get; private set; } = true;
    public bool MeasurementRemindersEnabled { get; private set; } = true;
    public bool WeeklySummaryRemindersEnabled { get; private set; } = true;
    public TimeOnly QuietHoursStart { get; private set; } = new(22, 0);
    public TimeOnly QuietHoursEnd { get; private set; } = new(7, 0);
    public int MaxRemindersPerDay { get; private set; } = 3;

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
        IReadOnlyList<string>? mealSlots = null,
        ActivityLevel? workActivityLevel = null,
        ActivityLevel? trainingActivityLevel = null,
        IReadOnlyList<MealScheduleEntry>? mealSchedule = null,
        int defaultRestSeconds = 90,
        int defaultRepetitions = 10,
        int defaultSets = 3,
        DayOfWeek weekStartsOn = DayOfWeek.Monday,
        string themePreference = "system",
        bool mealRemindersEnabled = false,
        int mealReminderMinutesBefore = 0,
        bool trainingRemindersEnabled = true,
        bool measurementRemindersEnabled = true,
        bool weeklySummaryRemindersEnabled = true,
        TimeOnly? quietHoursStart = null,
        TimeOnly? quietHoursEnd = null,
        int maxRemindersPerDay = 3)
    {
        TimeZoneId = timeZoneId;
        HeightCm = heightCm;
        StartingWeightKg = startingWeightKg;
        Goal = goal;
        PlannedTrainingsPerWeek = plannedTrainingsPerWeek;
        BirthDate = birthDate;
        Sex = sex;
        ActivityLevel = activityLevel;
        WorkActivityLevel = workActivityLevel ?? activityLevel;
        TrainingActivityLevel = trainingActivityLevel;
        TargetWeightKg = targetWeightKg;
        CalorieToleranceKcal = calorieToleranceKcal;
        if (mealSlots is not null)
        {
            var slots = mealSlots.Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Take(10).ToList();
            if (slots.Count > 0) MealSlots = string.Join('|', slots);
        }

        if (mealSchedule is not null)
        {
            var entries = mealSchedule
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .Take(10)
                .Select(x => $"{Clean(x.Name)}~{x.Time:HH\\:mm}~{(x.Enabled ? 1 : 0)}")
                .ToList();
            if (entries.Count > 0) MealSchedule = string.Join('|', entries);
        }

        DefaultRestSeconds = Math.Clamp(defaultRestSeconds, 15, 900);
        DefaultRepetitions = Math.Clamp(defaultRepetitions, 1, 100);
        DefaultSets = Math.Clamp(defaultSets, 1, 20);
        WeekStartsOn = weekStartsOn;
        ThemePreference = themePreference is "light" or "dark" ? themePreference : "system";
        MealRemindersEnabled = mealRemindersEnabled;
        MealReminderMinutesBefore = Math.Clamp(mealReminderMinutesBefore, 0, 180);
        TrainingRemindersEnabled = trainingRemindersEnabled;
        MeasurementRemindersEnabled = measurementRemindersEnabled;
        WeeklySummaryRemindersEnabled = weeklySummaryRemindersEnabled;
        QuietHoursStart = quietHoursStart ?? new TimeOnly(22, 0);
        QuietHoursEnd = quietHoursEnd ?? new TimeOnly(7, 0);
        MaxRemindersPerDay = Math.Clamp(maxRemindersPerDay, 1, 6);
    }

    private static string Clean(string value) => value.Trim().Replace("|", " ").Replace("~", " ");
}

public sealed record MealScheduleEntry(string Name, TimeOnly Time, bool Enabled);
