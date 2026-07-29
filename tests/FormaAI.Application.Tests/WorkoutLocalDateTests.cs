using FormaAI.Application.Training;

namespace FormaAI.Application.Tests;

public sealed class WorkoutLocalDateTests
{
    [Fact]
    public void ResolveUsesNoonInUsersTimeZone()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");

        var occurrence = WorkoutLocalDate.Resolve(
            new DateOnly(2026, 7, 20),
            zone,
            new DateTime(2026, 7, 29, 10, 0, 0, DateTimeKind.Utc));

        Assert.Equal(new DateTime(2026, 7, 20, 10, 0, 0, DateTimeKind.Utc), occurrence);
    }

    [Fact]
    public void ResolveRejectsDateAfterUsersLocalToday()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");

        Assert.Throws<ArgumentOutOfRangeException>(() => WorkoutLocalDate.Resolve(
            new DateOnly(2026, 7, 30),
            zone,
            new DateTime(2026, 7, 29, 10, 0, 0, DateTimeKind.Utc)));
    }
}
