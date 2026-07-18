using System.Security.Claims;
using FormaAI.Application.Progress;
using FormaAI.Contracts.Progress;
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
    public async Task<ActionResult<NutritionAdherenceResponse>> NutritionAdherence([FromQuery] DateOnly? month)
    {
        var selected = month ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var start = new DateOnly(selected.Year, selected.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        var userId = UserId();
        var meals = await db.Meals.Include(x => x.Items)
            .Where(x => x.UserId == userId && x.LocalDate >= start && x.LocalDate <= end)
            .ToListAsync();
        var targets = await db.NutritionTargets
            .Where(x => x.UserId == userId && x.EffectiveFrom <= end)
            .OrderBy(x => x.EffectiveFrom)
            .ToListAsync();
        var tolerance = await db.UserProfiles.Where(x => x.UserId == userId)
            .Select(x => x.CalorieToleranceKcal).SingleAsync();
        var fromUtc = start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = end.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var workoutDays = (await db.WorkoutSessions
                .Where(x => x.UserId == userId && x.Status == SessionStatus.Completed && x.FinishedAtUtc >= fromUtc && x.FinishedAtUtc < toUtc)
                .Select(x => x.FinishedAtUtc!.Value)
                .ToListAsync())
            .Select(DateOnly.FromDateTime)
            .ToHashSet();

        var days = Enumerable.Range(0, end.Day)
            .Select(offset => start.AddDays(offset))
            .Select(date =>
            {
                var target = targets.LastOrDefault(x => x.EffectiveFrom <= date);
                var logged = meals.Where(x => x.LocalDate == date).ToList();
                var items = logged.SelectMany(x => x.Items).ToList();
                var consumed = items.Sum(x => x.CaloriesKcal);
                var within = logged.Count > 0 && target is not null && Math.Abs(consumed - target.CaloriesKcal) <= tolerance;
                return new NutritionAdherencePoint(date, target?.CaloriesKcal, consumed, logged.Count > 0, within, workoutDays.Contains(date),
                    items.Sum(x => x.ProteinG), items.Sum(x => x.FatG), items.Sum(x => x.CarbohydratesG), target?.ProteinG, target?.FatG, target?.CarbohydratesG);
            }).ToList();

        return new NutritionAdherenceResponse(start, tolerance, days.Count(x => x.IsWithinTarget), days.Count(x => x.HasMeals), days);
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
                          join exercise in db.Exercises on item.ExerciseId equals exercise.Id
                          where session.UserId == UserId() && session.Status == SessionStatus.Completed && set.Type == SetType.Working
                              && set.CompletedAtUtc >= fromUtc && set.CompletedAtUtc < toUtc
                          select new { exercise.PrimaryMuscleGroup, set.WeightKg, set.Repetitions }).ToListAsync();
        return rows.GroupBy(x => x.PrimaryMuscleGroup).OrderByDescending(x => x.Count())
            .Select(x => new MuscleVolumePoint(x.Key.ToString(), x.Count(), x.Sum(y => y.WeightKg * y.Repetitions))).ToList();
    }

    [HttpGet("progress/week-summary")]
    public async Task<ActionResult<ProgressWeekSummaryResponse>> WeekSummary()
    {
        var userId = UserId();
        var to = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = to.AddDays(-6);
        var previousFrom = from.AddDays(-7);
        var fromUtc = previousFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var sessions = await db.WorkoutSessions
            .Include(x => x.Exercises).ThenInclude(x => x.Sets)
            .Where(x => x.UserId == userId && x.Status == SessionStatus.Completed && x.FinishedAtUtc >= fromUtc && x.FinishedAtUtc < toUtc)
            .ToListAsync();
        var currentSessions = sessions.Where(x => DateOnly.FromDateTime(x.FinishedAtUtc!.Value) >= from).ToList();
        var previousSessions = sessions.Where(x => DateOnly.FromDateTime(x.FinishedAtUtc!.Value) < from).ToList();
        var currentSets = currentSessions.SelectMany(x => x.Exercises).SelectMany(x => x.Sets).Where(x => x.Type == SetType.Working).ToList();
        var previousSets = previousSessions.SelectMany(x => x.Exercises).SelectMany(x => x.Sets).Where(x => x.Type == SetType.Working).ToList();
        var volume = currentSets.Sum(x => ProgressMetrics.Volume(x.WeightKg, x.Repetitions));
        var previousVolume = previousSets.Sum(x => ProgressMetrics.Volume(x.WeightKg, x.Repetitions));
        var plannedSets = currentSessions.SelectMany(x => x.Exercises).Sum(x => x.PlannedSets);
        var weeklyGoal = await db.UserProfiles.Where(x => x.UserId == userId).Select(x => x.PlannedTrainingsPerWeek).SingleAsync() ?? 0;
        var training = new TrainingWeekSummaryResponse(
            currentSessions.Count,
            weeklyGoal,
            (int)Math.Round(currentSessions.Sum(x => (x.FinishedAtUtc!.Value - x.StartedAtUtc).TotalMinutes)),
            currentSets.Count,
            plannedSets == 0 ? 0 : decimal.Round(currentSets.Count * 100m / plannedSets, 1),
            volume,
            previousVolume == 0 ? null : ProgressMetrics.PercentageChange(previousVolume, volume));

        var meals = await db.Meals.Include(x => x.Items)
            .Where(x => x.UserId == userId && x.LocalDate >= from && x.LocalDate <= to)
            .ToListAsync();
        var targets = await db.NutritionTargets.Where(x => x.UserId == userId && x.EffectiveFrom <= to)
            .OrderBy(x => x.EffectiveFrom).ToListAsync();
        var tolerance = await db.UserProfiles.Where(x => x.UserId == userId).Select(x => x.CalorieToleranceKcal).SingleAsync();
        var loggedDays = meals.GroupBy(x => x.LocalDate).Select(group =>
        {
            var target = targets.LastOrDefault(x => x.EffectiveFrom <= group.Key);
            var items = group.SelectMany(x => x.Items).ToList();
            return new
            {
                Calories = items.Sum(x => x.CaloriesKcal),
                Protein = items.Sum(x => x.ProteinG),
                TargetCalories = target?.CaloriesKcal ?? 0,
                TargetProtein = target?.ProteinG ?? 0,
                HasTarget = target is not null
            };
        }).ToList();
        var daysOnCalories = loggedDays.Count(x => x.HasTarget && Math.Abs(x.Calories - x.TargetCalories) <= tolerance);
        var nutrition = new NutritionWeekSummaryResponse(
            loggedDays.Count,
            daysOnCalories,
            loggedDays.Count == 0 ? 0 : decimal.Round(daysOnCalories * 100m / loggedDays.Count, 1),
            loggedDays.Count == 0 ? 0 : decimal.Round(loggedDays.Average(x => x.Calories)),
            loggedDays.Count == 0 ? 0 : decimal.Round(loggedDays.Average(x => x.TargetCalories)),
            loggedDays.Count(x => x.HasTarget && x.Protein >= x.TargetProtein),
            loggedDays.Count == 0 ? 0 : decimal.Round(loggedDays.Average(x => x.Protein), 1),
            loggedDays.Count == 0 ? 0 : decimal.Round(loggedDays.Average(x => x.Calories - x.TargetCalories)));

        return new ProgressWeekSummaryResponse(from, to, training, nutrition);
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

    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private static BodyMeasurementResponse MeasurementResponse(BodyMeasurement x) => new(x.Id, x.LocalDate, x.WeightKg, x.WaistCm, x.Notes,
        x.ChestCm, x.HipsCm, x.ArmCm, x.ThighCm);
}
