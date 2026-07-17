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
        var measurement = new BodyMeasurement(UserId(), request.LocalDate, request.WeightKg, request.WaistCm, request.Notes);
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
    private static BodyMeasurementResponse MeasurementResponse(BodyMeasurement x) => new(x.Id, x.LocalDate, x.WeightKg, x.WaistCm, x.Notes);
}
