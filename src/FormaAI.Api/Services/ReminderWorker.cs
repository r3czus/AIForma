using System.Globalization;
using System.Text.Json;
using FormaAI.Domain.Progress;
using FormaAI.Domain.Users;
using FormaAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using WebPush;

namespace FormaAI.Api.Services;

public sealed class ReminderWorker(IServiceScopeFactory scopes, VapidKeyStore vapid, ILogger<ReminderWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await SendDue(stoppingToken); }
            catch (Exception exception) { logger.LogError(exception, "Nie udało się przetworzyć przypomnień."); }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task SendDue(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var profiles = await db.UserProfiles.AsNoTracking().ToListAsync(cancellationToken);
        foreach (var profile in profiles)
        {
            var subscriptions = await db.PushSubscriptions.Where(x => x.UserId == profile.UserId).ToListAsync(cancellationToken);
            if (subscriptions.Count == 0) continue;
            var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(profile.TimeZoneId));
            if (InQuietHours(TimeOnly.FromDateTime(local), profile.QuietHoursStart, profile.QuietHoursEnd)) continue;
            var date = DateOnly.FromDateTime(local);
            if (await db.NotificationDeliveries.CountAsync(x => x.UserId == profile.UserId && x.LocalDate == date, cancellationToken) >= profile.MaxRemindersPerDay) continue;
            var message = await FindDueMessage(db, profile, local, date, cancellationToken);
            if (message is null || await db.NotificationDeliveries.AnyAsync(x => x.UserId == profile.UserId && x.LocalDate == date && x.EventKey == message.Value.Key, cancellationToken)) continue;

            var payload = JsonSerializer.Serialize(new { title = message.Value.Title, body = message.Value.Body, url = message.Value.Url, tag = message.Value.Key });
            var client = new WebPushClient();
            foreach (var subscription in subscriptions)
            {
                try { await client.SendNotificationAsync(new WebPush.PushSubscription(subscription.Endpoint, subscription.P256Dh, subscription.Auth), payload, vapid.Details, cancellationToken); }
                catch (WebPushException exception) { logger.LogWarning(exception, "Nie udało się dostarczyć powiadomienia {SubscriptionId}.", subscription.Id); }
            }
            db.NotificationDeliveries.Add(new NotificationDelivery(profile.UserId, date, message.Value.Key));
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task<(string Key, string Title, string Body, string Url)?> FindDueMessage(AppDbContext db, UserProfile profile, DateTime local, DateOnly date, CancellationToken cancellationToken)
    {
        var now = TimeOnly.FromDateTime(local);
        if (profile.MealRemindersEnabled)
        {
            foreach (var value in profile.MealSchedule.Split('|', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = value.Split('~');
                if (parts.Length != 3 || parts[2] != "1" || !TimeOnly.TryParseExact(parts[1], "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var mealTime)) continue;
                if (Math.Abs((now.ToTimeSpan() - mealTime.AddMinutes(-profile.MealReminderMinutesBefore).ToTimeSpan()).TotalMinutes) < 1)
                    return ($"meal-{parts[0]}", $"Pora na {parts[0].ToLowerInvariant()}", "Zapisz posiłek w kilku krokach.", $"/food/add?date={date:yyyy-MM-dd}&slot={Uri.EscapeDataString(parts[0])}");
            }
        }
        if (profile.TrainingRemindersEnabled && now.Hour == 9 && now.Minute == 0)
        {
            var planned = await db.TrainingPlans.Where(x => x.UserId == profile.UserId && x.IsActive).SelectMany(x => x.Days).AnyAsync(x => x.DayOfWeek == date.DayOfWeek, cancellationToken);
            if (planned) return ("training", "Dzisiaj masz trening", "Wybierz pełną sesję albo wariant 15 lub 30 minut.", "/training");
        }
        if (profile.MeasurementRemindersEnabled && now.Hour == 10 && now.Minute == 0)
        {
            var last = await db.BodyMeasurements.Where(x => x.UserId == profile.UserId).OrderByDescending(x => x.LocalDate).Select(x => (DateOnly?)x.LocalDate).FirstOrDefaultAsync(cancellationToken);
            if (last is null || last < date.AddDays(-14)) return ("measurement", "Czas na aktualny pomiar", "Pomiar z podobnej pory dnia pokaże trend dokładniej.", "/progress/measurements");
        }
        var finalDay = (DayOfWeek)(((int)profile.WeekStartsOn + 6) % 7);
        if (profile.WeeklySummaryRemindersEnabled && date.DayOfWeek == finalDay && now.Hour == 19 && now.Minute == 0)
            return ("weekly", "Zamknij swój tydzień", "Sprawdź kompletność danych i wybierz jeden priorytet.", "/progress/weekly");
        return null;
    }

    private static bool InQuietHours(TimeOnly current, TimeOnly from, TimeOnly to) => from <= to ? current >= from && current < to : current >= from || current < to;
}
