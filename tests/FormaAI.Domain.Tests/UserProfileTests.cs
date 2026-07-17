using FormaAI.Domain.Users;

namespace FormaAI.Domain.Tests;

public sealed class UserProfileTests
{
    [Fact]
    public void UpdateReplacesProfileValues()
    {
        var profile = new UserProfile("user-1", "UTC");

        profile.Update("Europe/Warsaw", 181, 83.5m, BodyGoal.Reduction, 4);

        Assert.Equal("Europe/Warsaw", profile.TimeZoneId);
        Assert.Equal(181, profile.HeightCm);
        Assert.Equal(83.5m, profile.StartingWeightKg);
        Assert.Equal(BodyGoal.Reduction, profile.Goal);
        Assert.Equal(4, profile.PlannedTrainingsPerWeek);
    }
}
