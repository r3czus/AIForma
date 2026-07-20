using FormaAI.Domain.Users;

namespace FormaAI.Domain.Tests;

public sealed class UserProfileTests
{
    [Fact]
    public void UpdateReplacesProfileValues()
    {
        var profile = new UserProfile("user-1", "UTC");

        profile.Update("Europe/Warsaw", 181, 83.5m, BodyGoal.Reduction, 4, weeklyWeightChangeKg: 0.7m);

        Assert.Equal("Europe/Warsaw", profile.TimeZoneId);
        Assert.Equal(181, profile.HeightCm);
        Assert.Equal(83.5m, profile.StartingWeightKg);
        Assert.Equal(BodyGoal.Reduction, profile.Goal);
        Assert.Equal(4, profile.PlannedTrainingsPerWeek);
        Assert.Equal(0.7m, profile.WeeklyWeightChangeKg);
    }

    [Fact]
    public void MaintenanceClearsWeeklyWeightChange()
    {
        var profile = new UserProfile("user-1", "UTC");

        profile.Update("UTC", 181, 83.5m, BodyGoal.Maintenance, 3, weeklyWeightChangeKg: 0.6m);

        Assert.Equal(0, profile.WeeklyWeightChangeKg);
    }

    [Fact]
    public void MealScheduleKeepsChosenIconAndColor()
    {
        var profile = new UserProfile("user-1", "UTC");
        var schedule = new[] { new MealScheduleEntry("Lunch", new TimeOnly(12, 30), true, "cup", "blue") };

        profile.Update("UTC", null, null, null, null, mealSchedule: schedule);

        Assert.Equal("Lunch~12:30~1~cup~blue", profile.MealSchedule);
    }
}
