using System.Security.Claims;
using System.Text.Json;
using FormaAI.Application.Assistant;
using FormaAI.Contracts.Progress;
using FormaAI.Domain.Nutrition;
using FormaAI.Domain.Progress;
using FormaAI.Domain.Training;
using FormaAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FormaAI.Api.Services;
using WebPush;

namespace FormaAI.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/coaching")]
public sealed class CoachingController(AppDbContext db, IAssistantModel assistant, IWebHostEnvironment environment, VapidKeyStore vapid) : ControllerBase
{
    [HttpGet("daily")]
    public async Task<DailyCoachResponse> Daily()
    {
        var userId = UserId();
        var today = await LocalToday(userId);
        var active = await db.WorkoutSessions.OrderByDescending(x => x.StartedAtUtc).FirstOrDefaultAsync(x => x.UserId == userId && x.Status == SessionStatus.InProgress);
        if (active is not null) return new("active-workout", "Wróć do rozpoczętego treningu", "Sesja czeka dokładnie w miejscu, w którym ją przerwałeś.", "Wróć do treningu", $"/workout/{active.Id}", "active_session");

        var profile = await db.UserProfiles.SingleAsync(x => x.UserId == userId);
        var plan = await db.TrainingPlans.Include(x => x.Days).SingleOrDefaultAsync(x => x.UserId == userId && x.IsActive);
        var day = plan?.Days.FirstOrDefault(x => x.DayOfWeek == today.DayOfWeek);
        if (day is not null)
        {
            var completed = await db.WorkoutSessions.AnyAsync(x => x.UserId == userId && x.TrainingDayId == day.Id && x.Status == SessionStatus.Completed && DateOnly.FromDateTime(x.FinishedAtUtc!.Value) == today);
            if (!completed) return new("planned-workout", $"Dzisiaj wypada {day.Name}", "Możesz wybrać pełną sesję albo krótszy wariant dopasowany do czasu.", "Wybierz wersję", "/training", "planned_today");
        }

        var status = await db.NutritionDayReviews.Where(x => x.UserId == userId && x.LocalDate == today).Select(x => x.Status).SingleOrDefaultAsync();
        var localHour = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(profile.TimeZoneId)).Hour;
        if (status == NutritionDayStatus.Unknown && localHour >= 19)
            return new("close-diary", "Domknij dzisiejszy dziennik", "Oznacz wpisy jako kompletne, częściowe albo brak danych. Dzięki temu tygodniowy raport nie będzie zgadywał.", "Sprawdź dziennik", "/food", "unknown_diary_evening");

        var lastMeasurement = await db.BodyMeasurements.Where(x => x.UserId == userId).OrderByDescending(x => x.LocalDate).Select(x => (DateOnly?)x.LocalDate).FirstOrDefaultAsync();
        if (lastMeasurement is null || lastMeasurement < today.AddDays(-14))
            return new("measurement", "Dodaj aktualny pomiar", "Regularny pomiar z podobnej pory dnia pomaga odróżnić trend od pojedynczego wahania.", "Dodaj pomiar", "/progress/measurements", "measurement_due");

        return new("done", "Najważniejsze rzeczy są pod kontrolą", "Nie musisz dodawać kolejnego obowiązku. Utrzymaj spokojny rytm.", "Zobacz progres", "/progress", "day_clear");
    }

    [HttpGet("momentum")]
    public async Task<MomentumResponse> Momentum()
    {
        var userId = UserId();
        var profile = await db.UserProfiles.SingleAsync(x => x.UserId == userId);
        var today = await LocalToday(userId);
        var weekStart = StartOfWeek(today, profile.WeekStartsOn);
        var historyStart = weekStart.AddDays(-77);
        var sessions = await db.WorkoutSessions.Where(x => x.UserId == userId && x.Status == SessionStatus.Completed && x.FinishedAtUtc >= historyStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
            .Select(x => x.FinishedAtUtc!.Value).ToListAsync();
        var goal = profile.PlannedTrainingsPerWeek ?? 0;
        var weeks = Enumerable.Range(0, 12).Select(i => weekStart.AddDays(-7 * i)).ToList();
        var counts = weeks.ToDictionary(x => x, x => sessions.Count(s => StartOfWeek(DateOnly.FromDateTime(s), profile.WeekStartsOn) == x));
        var successful = 0;
        foreach (var week in weeks.OrderByDescending(x => x))
        {
            if (goal > 0 && counts[week] >= goal) successful++;
            else if (week != weekStart) break;
        }
        var completeDays = await db.NutritionDayReviews.CountAsync(x => x.UserId == userId && x.LocalDate >= historyStart && x.Status == NutritionDayStatus.Complete);
        var checkIns = await db.WeeklyCheckIns.CountAsync(x => x.UserId == userId && x.LocalDate >= historyStart);
        return new(counts[weekStart], goal, successful, counts.Count(x => x.Value > 0), completeDays, checkIns);
    }

    [HttpGet("weekly/{weekStarting}")]
    public async Task<WeeklyReviewDataResponse> WeeklyData(DateOnly weekStarting)
    {
        var userId = UserId();
        var to = weekStarting.AddDays(6);
        var meals = await db.Meals.Include(x => x.Items).Where(x => x.UserId == userId && x.LocalDate >= weekStarting && x.LocalDate <= to).ToListAsync();
        var reviews = await db.NutritionDayReviews.Where(x => x.UserId == userId && x.LocalDate >= weekStarting && x.LocalDate <= to).ToDictionaryAsync(x => x.LocalDate, x => x.Status);
        var days = Enumerable.Range(0, 7).Select(i => weekStarting.AddDays(i)).Select(date =>
        {
            var logged = meals.Where(x => x.LocalDate == date).ToList();
            return new WeeklyDayQualityResponse(date, reviews.GetValueOrDefault(date), logged.Count, logged.SelectMany(x => x.Items).Sum(x => x.CaloriesKcal));
        }).ToList();
        var sessions = await db.WorkoutSessions.CountAsync(x => x.UserId == userId && x.Status == SessionStatus.Completed && x.FinishedAtUtc >= weekStarting.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) && x.FinishedAtUtc < to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        var goal = await db.UserProfiles.Where(x => x.UserId == userId).Select(x => x.PlannedTrainingsPerWeek).SingleAsync() ?? 0;
        var measurements = await db.BodyMeasurements.CountAsync(x => x.UserId == userId && x.LocalDate >= weekStarting && x.LocalDate <= to);
        var checkIn = await db.WeeklyCheckIns.AnyAsync(x => x.UserId == userId && x.LocalDate >= weekStarting && x.LocalDate <= to);
        return new(weekStarting, to, days, sessions, goal, measurements, checkIn);
    }

    [HttpPost("weekly")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<WeeklyReviewResponse>> SaveWeeklyReview(SaveWeeklyReviewRequest request, CancellationToken cancellationToken)
    {
        var userId = UserId();
        var existing = await db.WeeklyReviews.SingleOrDefaultAsync(x => x.UserId == userId && x.WeekStarting == request.WeekStarting, cancellationToken);
        if (existing is not null) db.WeeklyReviews.Remove(existing);
        var review = new WeeklyReview(userId, request.WeekStarting, request.Energy, request.Sleep, request.Hunger, request.Recovery, request.Stress, request.Notes, request.DeviationReasons);
        db.WeeklyReviews.Add(review);
        await db.SaveChangesAsync(cancellationToken);

        var data = await WeeklyData(request.WeekStarting);
        var prompt = JsonSerializer.Serialize(new { data.From, data.To, data.Days, data.CompletedWorkouts, data.PlannedWorkouts, data.Measurements, request.Energy, request.Sleep, request.Hunger, request.Recovery, request.Stress, request.Notes, request.DeviationReasons });
        try
        {
            var turn = await assistant.Generate(new AssistantModelRequest(
                "Jesteś spokojnym coachem FormaAI. Napisz po polsku krótkie podsumowanie: sukces, co pokazują dane, czego nie da się stwierdzić, jedna rzecz do poprawy i jedna propozycja na kolejny tydzień. Nie udawaj kompletności częściowych ani nieznanych dni. Maksymalnie 900 znaków.",
                [new AssistantModelMessage("user", prompt)], []), cancellationToken);
            if (!string.IsNullOrWhiteSpace(turn.Reply)) review.SetSummary(turn.Reply);
        }
        catch (AssistantModelUnavailableException)
        {
            review.SetSummary($"Wykonałeś {data.CompletedWorkouts} z {data.PlannedWorkouts} planowanych treningów. Kompletne dni dziennika: {data.Days.Count(x => x.Status == NutritionDayStatus.Complete)} z 7. W następnym tygodniu skup się na jednym działaniu, które najłatwiej utrzymać regularnie.");
        }
        await db.SaveChangesAsync(cancellationToken);
        return ReviewResponse(review);
    }

    [HttpGet("weekly/reviews")]
    public async Task<IReadOnlyList<WeeklyReviewResponse>> Reviews() => await db.WeeklyReviews.Where(x => x.UserId == UserId()).OrderByDescending(x => x.WeekStarting).Take(20).Select(x => ReviewResponse(x)).ToListAsync();

    [HttpGet("achievements")]
    public async Task<IReadOnlyList<AchievementResponse>> Achievements()
    {
        var userId = UserId();
        var result = new List<AchievementResponse>();
        var sessions = await db.WorkoutSessions.Where(x => x.UserId == userId && x.Status == SessionStatus.Completed).OrderBy(x => x.FinishedAtUtc).Select(x => x.FinishedAtUtc!.Value).ToListAsync();
        var measurements = await db.BodyMeasurements.Where(x => x.UserId == userId).OrderBy(x => x.LocalDate).Select(x => x.LocalDate).ToListAsync();
        var photos = await db.ProgressPhotos.Where(x => x.UserId == userId).OrderBy(x => x.LocalDate).Select(x => x.LocalDate).ToListAsync();
        if (sessions.Count > 0) result.Add(new("first-workout", "Pierwszy trening", "Pierwsza ukończona sesja jest początkiem historii progresu.", DateOnly.FromDateTime(sessions[0]), "Trening"));
        foreach (var threshold in new[] { 10, 25, 50, 100 }) if (sessions.Count >= threshold) result.Add(new($"workouts-{threshold}", $"{threshold} treningów za Tobą", $"Ukończyłeś {threshold} zapisanych sesji.", DateOnly.FromDateTime(sessions[threshold - 1]), "Regularność"));
        if (measurements.Count > 0) result.Add(new("first-measurement", "Pierwszy pomiar", "Masz punkt odniesienia dla trendu sylwetki.", measurements[0], "Sylwetka"));
        if (photos.Count > 0) result.Add(new("first-photo", "Pierwsze zdjęcie progresu", "Możesz porównywać zmiany poza samą masą ciała.", photos[0], "Sylwetka"));
        var completeDays = await db.NutritionDayReviews.Where(x => x.UserId == userId && x.Status == NutritionDayStatus.Complete).OrderBy(x => x.LocalDate).Select(x => x.LocalDate).ToListAsync();
        if (completeDays.Count > 0) result.Add(new("first-complete-diary", "Pierwszy kompletny dzień", "Dane żywieniowe tego dnia są świadomie zamknięte.", completeDays[0], "Jedzenie"));
        if (completeDays.Count >= 7) result.Add(new("seven-complete-days", "7 kompletnych dni", "Siedem wiarygodnych dni daje znacznie lepszy obraz odżywiania.", completeDays[6], "Jedzenie"));
        return result.OrderByDescending(x => x.EarnedOn).ToList();
    }

    [HttpGet("photos")]
    public async Task<IReadOnlyList<ProgressPhotoResponse>> Photos() => await db.ProgressPhotos.Where(x => x.UserId == UserId()).OrderByDescending(x => x.LocalDate).Select(x => PhotoResponse(x)).ToListAsync();

    [HttpPost("photos")]
    [ValidateAntiForgeryToken]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(15 * 1024 * 1024)]
    public async Task<ActionResult<ProgressPhotoResponse>> AddPhoto([FromForm] IFormFile photo, [FromForm] DateOnly localDate, [FromForm] ProgressPhotoPose pose, CancellationToken cancellationToken)
    {
        if (photo.Length is 0 or > 15 * 1024 * 1024 || !photo.ContentType.StartsWith("image/")) return BadRequest("Wybierz zdjęcie JPG, PNG, WEBP lub HEIC do 15 MB.");
        var storage = Path.Combine(environment.ContentRootPath, "App_Data", "progress-photos");
        Directory.CreateDirectory(storage);
        var extension = Path.GetExtension(photo.FileName);
        var name = $"{Guid.NewGuid():N}{extension}";
        await using (var stream = System.IO.File.Create(Path.Combine(storage, name))) await photo.CopyToAsync(stream, cancellationToken);
        var item = new ProgressPhoto(UserId(), localDate, pose, name, photo.ContentType);
        db.ProgressPhotos.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return Created($"api/v1/coaching/photos/{item.Id}", PhotoResponse(item));
    }

    [HttpGet("photos/{id:guid}/content")]
    public async Task<IActionResult> PhotoContent(Guid id)
    {
        var item = await db.ProgressPhotos.SingleOrDefaultAsync(x => x.Id == id && x.UserId == UserId());
        if (item is null) return NotFound();
        var path = Path.Combine(environment.ContentRootPath, "App_Data", "progress-photos", item.StorageName);
        return System.IO.File.Exists(path) ? PhysicalFile(path, item.ContentType) : NotFound();
    }

    [HttpDelete("photos/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePhoto(Guid id)
    {
        var item = await db.ProgressPhotos.SingleOrDefaultAsync(x => x.Id == id && x.UserId == UserId());
        if (item is null) return NotFound();
        var path = Path.Combine(environment.ContentRootPath, "App_Data", "progress-photos", item.StorageName);
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        db.ProgressPhotos.Remove(item);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("push-subscriptions")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePushSubscription(PushSubscriptionRequest request)
    {
        var old = await db.PushSubscriptions.SingleOrDefaultAsync(x => x.Endpoint == request.Endpoint);
        if (old is not null) db.PushSubscriptions.Remove(old);
        db.PushSubscriptions.Add(new FormaAI.Domain.Progress.PushSubscription(UserId(), request.Endpoint, request.P256Dh, request.Auth));
        await db.SaveChangesAsync();
        var client = new WebPushClient();
        var payload = JsonSerializer.Serialize(new { title = "FormaAI jest gotowa", body = "Powiadomienia mogą już docierać na ten telefon.", url = "/" });
        await client.SendNotificationAsync(new WebPush.PushSubscription(request.Endpoint, request.P256Dh, request.Auth), payload, vapid.Details);
        return NoContent();
    }

    [HttpGet("push-public-key")]
    [AllowAnonymous]
    public VapidPublicKeyResponse PushPublicKey() => new(vapid.PublicKey);

    private async Task<DateOnly> LocalToday(string userId)
    {
        var zone = await db.UserProfiles.Where(x => x.UserId == userId).Select(x => x.TimeZoneId).SingleAsync();
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(zone)));
    }

    private static DateOnly StartOfWeek(DateOnly date, DayOfWeek firstDay) => date.AddDays(-(7 + (int)date.DayOfWeek - (int)firstDay) % 7);
    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private static WeeklyReviewResponse ReviewResponse(WeeklyReview x) => new(x.Id, x.WeekStarting, x.Energy, x.Sleep, x.Hunger, x.Recovery, x.Stress, x.Notes, x.DeviationReasons, x.Summary);
    private static ProgressPhotoResponse PhotoResponse(ProgressPhoto x) => new(x.Id, x.LocalDate, x.Pose, $"api/v1/coaching/photos/{x.Id}/content");
}
