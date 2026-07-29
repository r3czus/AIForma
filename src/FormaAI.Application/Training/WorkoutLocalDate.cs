namespace FormaAI.Application.Training;

public static class WorkoutLocalDate
{
    public static DateTime Resolve(DateOnly localDate, TimeZoneInfo timeZone, DateTime utcNow)
    {
        var normalizedUtcNow = utcNow.Kind == DateTimeKind.Utc
            ? utcNow
            : DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        var localToday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(normalizedUtcNow, timeZone));
        if (localDate > localToday)
            throw new ArgumentOutOfRangeException(nameof(localDate), "Nie można zapisać treningu w przyszłości.");

        var localNoon = DateTime.SpecifyKind(localDate.ToDateTime(new TimeOnly(12, 0)), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(localNoon, timeZone);
    }
}
