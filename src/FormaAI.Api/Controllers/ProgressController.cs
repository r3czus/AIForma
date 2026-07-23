using System.Security.Claims;
using FormaAI.Application.Progress;
using FormaAI.Application.Nutrition;
using FormaAI.Contracts.Progress;
using FormaAI.Domain.Nutrition;
using FormaAI.Domain.Progress;
using FormaAI.Domain.Training;
using FormaAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormaAI.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class ProgressController(AppDbContext db) : ControllerBase
{
    [HttpPost("body-measurements")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<BodyMeasurementResponse>> AddMeasurement(SaveBodyMeasurementRequest request)
    {
        var measurement = new BodyMeasurement(UserId(), request.LocalDate, request.WeightKg, request.WaistCm, request.Notes,
            request.ChestCm, request.HipsCm, request.ArmCm, request.ThighCm);
        db.BodyMeasurements.Add(measurement);
        await db.SaveChangesAsync();
        return Created($"api/v1/body-measurements/{measurement.Id}", MeasurementResponse(measurement));
    }

    [HttpPut("body-measurements/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<BodyMeasurementResponse>> UpdateMeasurement(Guid id, SaveBodyMeasurementRequest request)
    {
        var measurement = await db.BodyMeasurements.SingleOrDefaultAsync(x => x.Id == id && x.UserId == UserId());
        if (measurement is null) return NotFound();
        measurement.Update(request.LocalDate, request.WeightKg, request.WaistCm, request.Notes,
            request.ChestCm, request.HipsCm, request.ArmCm, request.ThighCm);
        await db.SaveChangesAsync();
        return MeasurementResponse(measurement);
    }

    [HttpDelete("body-measurements/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMeasurement(Guid id)
    {
        var measurement = await db.BodyMeasurements.SingleOrDefaultAsync(x => x.Id == id && x.UserId == UserId());
        if (measurement is null) return NotFound();
        db.BodyMeasurements.Remove(measurement);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("progress/weight")]
    public async Task<ActionResult<WeightProgressResponse>> Weight([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var range = DateRange(from, to);
        if (range is null) return BadRequest("Nieprawidłowy zakres dat.");
        var measurements = await db.BodyMeasurements.Where(x => x.UserId == UserId() && x.LocalDate >= range.Value.From.AddDays(-6) && x.LocalDate <= range.Value.To)
            .OrderBy(x => x.LocalDate).ThenBy(x => x.MeasuredAtUtc).ToListAsync();
        var daily = measurements.GroupBy(x => x.LocalDate).ToDictionary(x => x.Key, x => x.Last().WeightKg);
        var points = daily.Where(x => x.Key >= range.Value.From).OrderBy(x => x.Key).Select(x =>
        {
            var average = daily.Where(y => y.Key >= x.Key.AddDays(-6) && y.Key <= x.Key).Average(y => y.Value);
            return new WeightProgressPoint(x.Key, x.Value, decimal.Round(average, 2));
        }).ToList();
        decimal? change = points.Count < 2 ? null : points[^1].SevenDayAverageKg - points[0].SevenDayAverageKg;
        return new WeightProgressResponse(points, change, points.LastOrDefault()?.SevenDayAverageKg);
    }

    [HttpGet("progress/exercises/{exerciseId:guid}")]
    public async Task<ActionResult<ExerciseProgressResponse>> Exercise(Guid exerciseId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var range = DateRange(from, to);
        if (range is null) return BadRequest("Nieprawidłowy zakres dat.");
        var userId = UserId();
        var exercise = await db.Exercises.SingleOrDefaultAsync(x => x.Id == exerciseId && x.IsActive && (x.OwnerUserId == null || x.OwnerUserId == userId));
        if (exercise is null) return NotFound();
        var fromUtc = range.Value.From.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = range.Value.To.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var sets = await (from set in db.CompletedSets
                          join workoutExercise in db.WorkoutExercises on set.WorkoutExerciseId equals workoutExercise.Id
                          join session in db.WorkoutSessions on workoutExercise.WorkoutSessionId equals session.Id
                          where session.UserId == userId && session.Status == SessionStatus.Completed && workoutExercise.ExerciseId == exerciseId
                              && set.Type == SetType.Working && set.CompletedAtUtc >= fromUtc && set.CompletedAtUtc < toUtc
                          select new { set.CompletedAtUtc, set.WeightKg, set.Repetitions }).ToListAsync();
        var points = sets.GroupBy(x => DateOnly.FromDateTime(x.CompletedAtUtc)).OrderBy(x => x.Key)
            .Select(x => new ExerciseProgressPoint(x.Key,
                x.Sum(y => ProgressMetrics.Volume(y.WeightKg, y.Repetitions)),
                x.Select(y => ProgressMetrics.EstimatedOneRepMax(y.WeightKg, y.Repetitions)).Where(y => y.HasValue).Max()))
            .ToList();
        decimal? volumeChange = points.Count < 2 ? null : ProgressMetrics.PercentageChange(points[0].VolumeKg, points[^1].VolumeKg);
        var e1Rm = points.Where(x => x.EstimatedOneRepMaxKg.HasValue).ToList();
        var e1RmChange = e1Rm.Count < 2 ? null : e1Rm[^1].EstimatedOneRepMaxKg - e1Rm[0].EstimatedOneRepMaxKg;
        return new ExerciseProgressResponse(exercise.Id, exercise.Name, points, volumeChange, e1RmChange);
    }

    [HttpGet("progress/consistency")]
    public async Task<ActionResult<ConsistencyResponse>> Consistency([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var range = DateRange(from, to);
        if (range is null) return BadRequest("Nieprawidłowy zakres dat.");
        var userId = UserId();
        var fromUtc = range.Value.From.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = range.Value.To.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dates = await db.WorkoutSessions.Where(x => x.UserId == userId && x.Status == SessionStatus.Completed && x.FinishedAtUtc >= fromUtc && x.FinishedAtUtc < toUtc)
            .Select(x => x.FinishedAtUtc!.Value).ToListAsync();
        var weeklyGoal = await db.UserProfiles.Where(x => x.UserId == userId).Select(x => x.PlannedTrainingsPerWeek).SingleAsync() ?? 0;
        var days = range.Value.To.DayNumber - range.Value.From.DayNumber + 1;
        var planned = (int)Math.Ceiling(weeklyGoal * days / 7m);
        var weeks = dates.GroupBy(x => StartOfWeek(DateOnly.FromDateTime(x))).OrderBy(x => x.Key).Select(x => new ConsistencyPoint(x.Key, x.Count())).ToList();
        var percent = planned == 0 ? 0 : decimal.Round(dates.Count * 100m / planned, 1);
        return new ConsistencyResponse(dates.Count, planned, percent, weeks);
    }

    [HttpGet("progress/nutrition-adherence")]
    public async Task<ActionResult<NutritionAdherenceResponse>> NutritionAdherence([FromQuery] DateOnly? month, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var selected = month ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var start = from ?? new DateOnly(selected.Year, selected.Month, 1);
        var end = to ?? start.AddMonths(1).AddDays(-1);
        if (start > end || end.DayNumber - start.DayNumber > 366) return BadRequest("Nieprawidłowy zakres dat.");
        var userId = UserId();
        var meals = await db.Meals.Include(x => x.Items)
            .Where(x => x.UserId == userId && x.LocalDate >= start && x.LocalDate <= end)
            .ToListAsync();
        var targets = await db.NutritionTargets
            .Where(x => x.UserId == userId && x.EffectiveFrom <= end)
            .OrderBy(x => x.EffectiveFrom)
            .ToListAsync();
        var profile = await db.UserProfiles.Where(x => x.UserId == userId)
            .Select(x => new { x.CalorieToleranceKcal, x.TrainingActivityLevel, x.TimeZoneId }).SingleAsync();
        var zone = TimeZoneInfo.FindSystemTimeZoneById(profile.TimeZoneId);
        var fromUtc = TimeZoneInfo.ConvertTimeToUtc(start.ToDateTime(TimeOnly.MinValue), zone);
        var toUtc = TimeZoneInfo.ConvertTimeToUtc(end.AddDays(1).ToDateTime(TimeOnly.MinValue), zone);
        var workoutSessions = await db.WorkoutSessions
                .Include(x => x.Exercises).ThenInclude(x => x.Sets)
                .Where(x => x.UserId == userId && x.Status == SessionStatus.Completed && x.FinishedAtUtc >= fromUtc && x.FinishedAtUtc < toUtc)
                .ToListAsync();
        var allSessions = await db.WorkoutSessions.Where(x => x.UserId == userId && x.Status == SessionStatus.Completed)
            .OrderBy(x => x.FinishedAtUtc).Select(x => x.FinishedAtUtc!.Value).ToListAsync();
        var measurements = await db.BodyMeasurements.Where(x => x.UserId == userId).OrderBy(x => x.LocalDate).Select(x => x.LocalDate).ToListAsync();
        var photos = await db.ProgressPhotos.Where(x => x.UserId == userId).OrderBy(x => x.LocalDate).Select(x => x.LocalDate).ToListAsync();
        var completeDays = await db.NutritionDayReviews.Where(x => x.UserId == userId && x.Status == NutritionDayStatus.Complete)
            .OrderBy(x => x.LocalDate).Select(x => x.LocalDate).ToListAsync();
        var achievements = new List<(DateOnly? Date, string Title)>
        {
            (DateAt(allSessions, 1), "Pierwszy trening"),
            (DateAt(allSessions, 10), "10 treningów za Tobą"),
            (DateAt(allSessions, 25), "25 treningów za Tobą"),
            (DateAt(allSessions, 50), "50 treningów za Tobą"),
            (DateAt(allSessions, 100), "100 treningów za Tobą"),
            (DateAt(measurements, 1), "Pierwszy pomiar"),
            (DateAt(photos, 1), "Pierwsze zdjęcie progresu"),
            (DateAt(completeDays, 1), "Pierwszy kompletny dzień"),
            (DateAt(completeDays, 7), "7 kompletnych dni")
        };

        var days = Enumerable.Range(0, end.DayNumber - start.DayNumber + 1)
            .Select(offset => start.AddDays(offset))
            .Select(date =>
            {
                var target = targets.LastOrDefault(x => x.EffectiveFrom <= date);
                var logged = meals.Where(x => x.LocalDate == date).ToList();
                var items = logged.SelectMany(x => x.Items).ToList();
                var consumed = items.Sum(x => x.CaloriesKcal);
                var sessions = workoutSessions.Where(x => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(x.FinishedAtUtc!.Value, zone)) == date).ToList();
                var sets = sessions.SelectMany(x => x.Exercises).SelectMany(x => x.Sets).Where(x => x.Type == SetType.Working).ToList();
                var bonus = sessions.Sum(x => NutritionCalculator.TrainingBonus(profile.TrainingActivityLevel, x.FinishedAtUtc!.Value - x.StartedAtUtc,
                    x.Exercises.SelectMany(e => e.Sets).Count(s => s.Type == SetType.Working)));
                var targetCalories = target?.CaloriesKcal + bonus;
                var within = logged.Count > 0 && targetCalories is not null && Math.Abs(consumed - targetCalories.Value) <= profile.CalorieToleranceKcal;
                var workoutDetails = sessions.SelectMany(x => x.Exercises).Select(x =>
                {
                    var working = x.Sets.Where(s => s.Type == SetType.Working).ToList();
                    return new WorkoutDayExerciseResponse(x.ExerciseNameSnapshot, working.Count, working.Sum(s => ProgressMetrics.Volume(s.WeightKg, s.Repetitions)));
                }).ToList();
                return new NutritionAdherencePoint(date, targetCalories, consumed, logged.Count > 0, within, sessions.Count > 0,
                    items.Sum(x => x.ProteinG), items.Sum(x => x.FatG), items.Sum(x => x.CarbohydratesG), target?.ProteinG, target?.FatG, target?.CarbohydratesG + bonus / 4m,
                    sessions.Count == 1 ? sessions[0].NameSnapshot : sessions.Count > 1 ? $"{sessions.Count} treningi" : null,
                    (int)Math.Round(sessions.Sum(x => (x.FinishedAtUtc!.Value - x.StartedAtUtc).TotalMinutes)),
                    sessions.Sum(x => x.Exercises.Count), sets.Count, sets.Sum(x => ProgressMetrics.Volume(x.WeightKg, x.Repetitions)),
                    achievements.Where(x => x.Date == date).Select(x => x.Title).ToList(), workoutDetails, sessions.FirstOrDefault()?.Id);
            }).ToList();

        return new NutritionAdherenceResponse(start, profile.CalorieToleranceKcal, days.Count(x => x.IsWithinTarget), days.Count(x => x.HasMeals), days);
    }

    [HttpGet("progress/check-ins")]
    public async Task<IReadOnlyList<WeeklyCheckInResponse>> CheckIns([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var range = DateRange(from, to);
        if (range is null) return [];
        return await db.WeeklyCheckIns.Where(x => x.UserId == UserId() && x.LocalDate >= range.Value.From && x.LocalDate <= range.Value.To)
            .OrderBy(x => x.LocalDate).Select(x => new WeeklyCheckInResponse(x.Id, x.LocalDate, x.Energy, x.Sleep, x.Hunger, x.Recovery, x.Notes)).ToListAsync();
    }

    [HttpPost("progress/check-ins")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<WeeklyCheckInResponse>> SaveCheckIn(SaveWeeklyCheckInRequest request)
    {
        var userId = UserId();
        var existing = await db.WeeklyCheckIns.SingleOrDefaultAsync(x => x.UserId == userId && x.LocalDate == request.LocalDate);
        if (existing is not null) db.WeeklyCheckIns.Remove(existing);
        var checkIn = new WeeklyCheckIn(userId, request.LocalDate, request.Energy, request.Sleep, request.Hunger, request.Recovery, request.Notes);
        db.WeeklyCheckIns.Add(checkIn);
        await db.SaveChangesAsync();
        return new WeeklyCheckInResponse(checkIn.Id, checkIn.LocalDate, checkIn.Energy, checkIn.Sleep, checkIn.Hunger, checkIn.Recovery, checkIn.Notes);
    }

    [HttpGet("progress/body-measurements")]
    public async Task<ActionResult<IReadOnlyList<BodyMeasurementResponse>>> BodyMeasurements([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var range = DateRange(from, to);
        if (range is null) return BadRequest("Nieprawidłowy zakres dat.");
        var measurements = await db.BodyMeasurements.Where(x => x.UserId == UserId() && x.LocalDate >= range.Value.From && x.LocalDate <= range.Value.To)
            .OrderBy(x => x.LocalDate).ThenBy(x => x.MeasuredAtUtc).ToListAsync();
        return measurements.Select(MeasurementResponse).ToList();
    }

    [HttpGet("progress/muscle-volume")]
    public async Task<ActionResult<IReadOnlyList<MuscleVolumePoint>>> MuscleVolume([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var range = DateRange(from, to);
        if (range is null) return BadRequest("Nieprawidłowy zakres dat.");
        var fromUtc = range.Value.From.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = range.Value.To.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var rows = await (from set in db.CompletedSets
                          join item in db.WorkoutExercises on set.WorkoutExerciseId equals item.Id
                          join session in db.WorkoutSessions on item.WorkoutSessionId equals session.Id
                          join engagement in db.WorkoutExerciseMuscleEngagements on item.Id equals engagement.WorkoutExerciseId
                          where session.UserId == UserId() && session.Status == SessionStatus.Completed && set.Type == SetType.Working
                              && set.CompletedAtUtc >= fromUtc && set.CompletedAtUtc < toUtc
                          select new { engagement.MuscleGroup, engagement.Percentage, set.WeightKg, set.Repetitions }).ToListAsync();
        var groups = rows.GroupBy(x => x.MuscleGroup).Select(x => new
        {
            x.Key,
            Sets = x.Sum(y => y.Percentage / 100m),
            Volume = x.Sum(y => y.WeightKg * y.Repetitions * y.Percentage / 100m)
        }).OrderByDescending(x => x.Sets).ToList();
        var total = groups.Sum(x => x.Sets);
        return groups.Select(x => new MuscleVolumePoint(x.Key.ToString(), decimal.Round(x.Sets, 1), decimal.Round(x.Volume),
            total == 0 ? 0 : decimal.Round(x.Sets * 100m / total, 1))).ToList();
    }

    [HttpGet("progress/week-summary")]
    public async Task<ActionResult<ProgressWeekSummaryResponse>> WeekSummary([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var range = DateRange(from, to);
        if (range is null) return BadRequest("Nieprawidłowy zakres dat.");
        var userId = UserId();
        var periodFrom = range.Value.From;
        var periodTo = range.Value.To;
        var profile = await db.UserProfiles.Where(x => x.UserId == userId)
            .Select(x => new { x.CalorieToleranceKcal, x.TrainingActivityLevel, x.TimeZoneId }).SingleAsync();
        var zone = TimeZoneInfo.FindSystemTimeZoneById(profile.TimeZoneId);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone));
        var effectiveTo = periodTo < today ? periodTo : today;
        var periodDays = effectiveTo < periodFrom ? 0 : effectiveTo.DayNumber - periodFrom.DayNumber + 1;
        var previousFrom = periodFrom.AddDays(-periodDays);
        var fromUtc = previousFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = effectiveTo.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var sessions = await db.WorkoutSessions
            .Include(x => x.Exercises).ThenInclude(x => x.Sets)
            .Where(x => x.UserId == userId && x.Status == SessionStatus.Completed && x.FinishedAtUtc >= fromUtc && x.FinishedAtUtc < toUtc)
            .ToListAsync();
        var currentSessions = sessions.Where(x =>
        {
            var date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(x.FinishedAtUtc!.Value, zone));
            return date >= periodFrom && date <= effectiveTo;
        }).ToList();
        var previousSessions = sessions.Except(currentSessions).ToList();
        var currentSets = currentSessions.SelectMany(x => x.Exercises).SelectMany(x => x.Sets).Where(x => x.Type == SetType.Working).ToList();
        var previousSets = previousSessions.SelectMany(x => x.Exercises).SelectMany(x => x.Sets).Where(x => x.Type == SetType.Working).ToList();
        var volume = currentSets.Sum(x => ProgressMetrics.Volume(x.WeightKg, x.Repetitions));
        var previousVolume = previousSets.Sum(x => ProgressMetrics.Volume(x.WeightKg, x.Repetitions));
        var plannedSets = currentSessions.SelectMany(x => x.Exercises).Sum(x => x.PlannedSets);
        var weeklyGoal = await db.UserProfiles.Where(x => x.UserId == userId).Select(x => x.PlannedTrainingsPerWeek).SingleAsync() ?? 0;
        var training = new TrainingWeekSummaryResponse(
            currentSessions.Count,
            (int)Math.Ceiling(weeklyGoal * periodDays / 7m),
            (int)Math.Round(currentSessions.Sum(x => (x.FinishedAtUtc!.Value - x.StartedAtUtc).TotalMinutes)),
            currentSets.Count,
            plannedSets == 0 ? 0 : decimal.Round(currentSets.Count * 100m / plannedSets, 1),
            volume,
            previousVolume == 0 ? null : ProgressMetrics.PercentageChange(previousVolume, volume));

        var meals = await db.Meals.Include(x => x.Items)
            .Where(x => x.UserId == userId && x.LocalDate >= periodFrom && x.LocalDate <= periodTo)
            .ToListAsync();
        var targets = await db.NutritionTargets.Where(x => x.UserId == userId && x.EffectiveFrom <= periodTo)
            .OrderBy(x => x.EffectiveFrom).ToListAsync();
        var eligibleRange = ProgressPeriodCalculator.EligibleRange(periodFrom, periodTo, today, targets.FirstOrDefault()?.EffectiveFrom);
        var eligibleDays = eligibleRange is null ? 0 : eligibleRange.Value.To.DayNumber - eligibleRange.Value.From.DayNumber + 1;
        var loggedDays = meals.GroupBy(x => x.LocalDate).Select(group =>
        {
            var target = targets.LastOrDefault(x => x.EffectiveFrom <= group.Key);
            var items = group.SelectMany(x => x.Items).ToList();
            var daySessions = currentSessions.Where(x => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(x.FinishedAtUtc!.Value, zone)) == group.Key).ToList();
            var bonus = daySessions.Sum(x => NutritionCalculator.TrainingBonus(profile.TrainingActivityLevel, x.FinishedAtUtc!.Value - x.StartedAtUtc,
                x.Exercises.SelectMany(e => e.Sets).Count(s => s.Type == SetType.Working)));
            return new
            {
                Calories = items.Sum(x => x.CaloriesKcal),
                Protein = items.Sum(x => x.ProteinG),
                TargetCalories = (target?.CaloriesKcal ?? 0) + bonus,
                TargetProtein = target?.ProteinG ?? 0,
                HasTarget = target is not null
            };
        }).ToList();
        var daysOnCalories = loggedDays.Count(x => x.HasTarget && Math.Abs(x.Calories - x.TargetCalories) <= profile.CalorieToleranceKcal);
        var nutrition = new NutritionWeekSummaryResponse(
            loggedDays.Count,
            eligibleDays,
            daysOnCalories,
            ProgressPeriodCalculator.AdherencePercent(daysOnCalories, eligibleDays),
            loggedDays.Count == 0 ? 0 : decimal.Round(loggedDays.Average(x => x.Calories)),
            loggedDays.Count == 0 ? 0 : decimal.Round(loggedDays.Average(x => x.TargetCalories)),
            loggedDays.Count(x => x.HasTarget && x.Protein >= x.TargetProtein),
            loggedDays.Count == 0 ? 0 : decimal.Round(loggedDays.Average(x => x.Protein), 1),
            loggedDays.Count == 0 ? 0 : decimal.Round(loggedDays.Average(x => x.Calories - x.TargetCalories)));

        return new ProgressWeekSummaryResponse(periodFrom, periodTo, training, nutrition);
    }

    private static (DateOnly From, DateOnly To)? DateRange(DateOnly? from, DateOnly? to)
    {
        var end = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var start = from ?? end.AddDays(-89);
        return start > end || end.DayNumber - start.DayNumber > 366 ? null : (start, end);
    }

    private static DateOnly StartOfWeek(DateOnly date)
    {
        var offset = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-offset);
    }

    private static DateOnly? DateAt(IReadOnlyList<DateOnly> dates, int count) => dates.Count >= count ? dates[count - 1] : null;
    private static DateOnly? DateAt(IReadOnlyList<DateTime> dates, int count) => dates.Count >= count ? DateOnly.FromDateTime(dates[count - 1]) : null;

    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private static BodyMeasurementResponse MeasurementResponse(BodyMeasurement x) => new(x.Id, x.LocalDate, x.WeightKg, x.WaistCm, x.Notes,
        x.ChestCm, x.HipsCm, x.ArmCm, x.ThighCm);
}
