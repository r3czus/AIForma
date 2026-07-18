using System.Security.Claims;
using FormaAI.Contracts.Training;
using FormaAI.Domain.Training;
using FormaAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormaAI.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class TrainingController(AppDbContext db) : ControllerBase
{
    [HttpGet("exercises")]
    public async Task<IReadOnlyList<ExerciseResponse>> Exercises([FromQuery] string? query)
    {
        var userId = UserId();
        var exercises = db.Exercises.Where(x => x.IsActive && (x.OwnerUserId == null || x.OwnerUserId == userId));
        if (!string.IsNullOrWhiteSpace(query)) exercises = exercises.Where(x => x.Name.Contains(query));
        return await exercises.OrderBy(x => x.Name).Take(50)
            .Select(x => new ExerciseResponse(x.Id, x.Name, x.PrimaryMuscleGroup, x.Equipment, x.IsUnilateral, x.OwnerUserId != null, x.Description))
            .ToListAsync();
    }

    [HttpPost("exercises")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<ExerciseResponse>> CreateExercise(SaveExerciseRequest request)
    {
        var exercise = new Exercise(UserId(), request.Name, request.MuscleGroup, request.Equipment, request.IsUnilateral, request.Description);
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();
        return Created($"api/v1/exercises/{exercise.Id}", ExerciseResponse(exercise));
    }

    [HttpPut("exercises/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<ExerciseResponse>> UpdateExercise(Guid id, SaveExerciseRequest request)
    {
        var exercise = await db.Exercises.SingleOrDefaultAsync(x => x.Id == id && x.OwnerUserId == UserId());
        if (exercise is null) return NotFound();
        exercise.Update(request.Name, request.MuscleGroup, request.Equipment, request.IsUnilateral, request.Description);
        await db.SaveChangesAsync();
        return ExerciseResponse(exercise);
    }

    [HttpGet("training-plans")]
    public async Task<IReadOnlyList<TrainingPlanResponse>> Plans()
    {
        var plans = await db.TrainingPlans.Include(x => x.Days).ThenInclude(x => x.Exercises)
            .Where(x => x.UserId == UserId()).OrderByDescending(x => x.IsActive).ThenBy(x => x.Name).ToListAsync();
        return await PlanResponses(plans);
    }

    [HttpGet("training-plans/active")]
    public async Task<ActionResult<TrainingPlanResponse>> ActivePlan()
    {
        var plan = await db.TrainingPlans.Include(x => x.Days).ThenInclude(x => x.Exercises)
            .SingleOrDefaultAsync(x => x.UserId == UserId() && x.IsActive);
        if (plan is null) return NotFound();
        return (await PlanResponses([plan])).Single();
    }

    [HttpPost("training-plans")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<TrainingPlanResponse>> CreatePlan(SaveTrainingPlanRequest request)
    {
        var plan = await BuildPlan(UserId(), request);
        if (plan is null) return ValidationProblem("Plan zawiera niedostępne ćwiczenie lub nieprawidłowy zakres powtórzeń.");
        db.TrainingPlans.Add(plan);
        await db.SaveChangesAsync();
        return Created($"api/v1/training-plans/{plan.Id}", (await PlanResponses([plan])).Single());
    }

    [HttpPut("training-plans/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<TrainingPlanResponse>> UpdatePlan(Guid id, SaveTrainingPlanRequest request)
    {
        var userId = UserId();
        var plan = await db.TrainingPlans.AsNoTracking().Include(x => x.Days).ThenInclude(x => x.Exercises)
            .SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (plan is null) return NotFound();
        var changed = await BuildPlan(userId, request);
        if (changed is null) return ValidationProblem("Plan zawiera niedostępne ćwiczenie lub nieprawidłowy zakres powtórzeń.");

        await using var transaction = await db.Database.BeginTransactionAsync();
        var currentDays = plan.Days.OrderBy(x => x.SequenceNumber).ToList();
        var currentIds = currentDays.Select(x => x.Id).ToList();
        await db.PlannedExercises.Where(x => currentIds.Contains(x.TrainingDayId)).ExecuteDeleteAsync();
        await db.TrainingPlans.Where(x => x.Id == plan.Id).ExecuteUpdateAsync(x => x
            .SetProperty(p => p.Name, changed.Name)
            .SetProperty(p => p.Goal, changed.Goal)
            .SetProperty(p => p.StartsOn, changed.StartsOn)
            .SetProperty(p => p.UpdatedAtUtc, DateTime.UtcNow));

        for (var i = 0; i < changed.Days.Count; i++)
        {
            var source = changed.Days[i];
            if (i < currentDays.Count)
            {
                var day = currentDays[i];
                await db.TrainingDays.Where(x => x.Id == day.Id).ExecuteUpdateAsync(x => x
                    .SetProperty(d => d.Name, source.Name)
                    .SetProperty(d => d.DayOfWeek, source.DayOfWeek)
                    .SetProperty(d => d.SequenceNumber, i + 1));
            }
        }

        if (currentDays.Count > changed.Days.Count)
        {
            var removed = currentDays.Skip(changed.Days.Count).ToList();
            var ids = removed.Select(x => x.Id).ToList();
            await db.WorkoutSessions.Where(x => x.TrainingDayId.HasValue && ids.Contains(x.TrainingDayId.Value))
                .ExecuteUpdateAsync(x => x.SetProperty(s => s.TrainingDayId, (Guid?)null));
            await db.TrainingDays.Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync();
        }

        db.ChangeTracker.Clear();
        var tracked = await db.TrainingPlans.Include(x => x.Days).SingleAsync(x => x.Id == id);
        var kept = tracked.Days.OrderBy(x => x.SequenceNumber).ToList();
        for (var i = 0; i < changed.Days.Count; i++)
        {
            if (i >= kept.Count) { tracked.Days.Add(changed.Days[i]); db.TrainingDays.Add(changed.Days[i]); }
            else kept[i].Exercises.AddRange(changed.Days[i].Exercises);
        }
        db.PlannedExercises.AddRange(changed.Days.SelectMany(x => x.Exercises));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return (await PlanResponses([tracked])).Single();
    }

    [HttpPost("training-plans/{id:guid}/activate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivatePlan(Guid id)
    {
        var userId = UserId();
        var plans = await db.TrainingPlans.Where(x => x.UserId == userId).ToListAsync();
        var selected = plans.SingleOrDefault(x => x.Id == id);
        if (selected is null) return NotFound();
        foreach (var plan in plans) plan.IsActive = plan.Id == id;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("workouts/today")]
    public async Task<ActionResult<TodayWorkoutResponse>> Today()
    {
        var userId = UserId();
        var plan = await db.TrainingPlans.Include(x => x.Days).ThenInclude(x => x.Exercises)
            .SingleOrDefaultAsync(x => x.UserId == userId && x.IsActive);
        if (plan is null) return NotFound();
        var zoneId = await db.UserProfiles.Where(x => x.UserId == userId).Select(x => x.TimeZoneId).SingleAsync();
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(zoneId));
        var day = plan.Days.FirstOrDefault(x => x.DayOfWeek == localNow.DayOfWeek)
            ?? plan.Days.Where(x => x.DayOfWeek == null).OrderBy(x => x.SequenceNumber).FirstOrDefault();
        if (day is null) return NotFound();
        var from = TimeZoneInfo.ConvertTimeToUtc(localNow.Date, TimeZoneInfo.FindSystemTimeZoneById(zoneId));
        var completed = await db.WorkoutSessions.AnyAsync(x => x.UserId == userId && x.TrainingDayId == day.Id && x.Status == SessionStatus.Completed && x.StartedAtUtc >= from && x.StartedAtUtc < from.AddDays(1));
        var exercises = await PlannedResponses(day.Exercises);
        return new TodayWorkoutResponse(plan.Id, day.Id, plan.Name, day.Name, completed, exercises);
    }

    [HttpPost("workout-sessions")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<WorkoutSessionResponse>> Start(StartWorkoutRequest request)
    {
        var userId = UserId();
        var active = await SessionQuery().OrderByDescending(x => x.StartedAtUtc).FirstOrDefaultAsync(x => x.UserId == userId && x.Status == SessionStatus.InProgress);
        if (active is not null) return SessionResponse(active);
        var day = await db.TrainingDays.Include(x => x.Exercises)
            .SingleOrDefaultAsync(x => x.Id == request.TrainingDayId && db.TrainingPlans.Any(p => p.Id == x.TrainingPlanId && p.UserId == userId));
        if (day is null) return NotFound();
        var plan = await db.TrainingPlans.SingleAsync(x => x.Id == day.TrainingPlanId);
        var ids = day.Exercises.Select(x => x.ExerciseId).ToList();
        var catalog = await db.Exercises.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        var session = new WorkoutSession(userId, plan, day);
        foreach (var planned in day.Exercises.OrderBy(x => x.Order)) session.Exercises.Add(new WorkoutExercise(planned, catalog[planned.ExerciseId]));
        db.WorkoutSessions.Add(session);
        await db.SaveChangesAsync();
        return Created($"api/v1/workout-sessions/{session.Id}", SessionResponse(session));
    }

    [HttpGet("workout-sessions/active")]
    public async Task<ActionResult<WorkoutSessionResponse>> ActiveSession()
    {
        var session = await SessionQuery().OrderByDescending(x => x.StartedAtUtc).FirstOrDefaultAsync(x => x.UserId == UserId() && x.Status == SessionStatus.InProgress);
        return session is null ? NotFound() : SessionResponse(session);
    }

    [HttpGet("workout-sessions/{id:guid}")]
    public async Task<ActionResult<WorkoutSessionResponse>> Session(Guid id)
    {
        var session = await SessionQuery().SingleOrDefaultAsync(x => x.Id == id && x.UserId == UserId());
        return session is null ? NotFound() : SessionResponse(session);
    }

    [HttpPost("workout-sessions/{id:guid}/sets")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<CompletedSetResponse>> AddSet(Guid id, SaveSetRequest request)
    {
        var session = await SessionQuery().SingleOrDefaultAsync(x => x.Id == id && x.UserId == UserId());
        if (session is null) return NotFound();
        if (session.Status != SessionStatus.InProgress) return Conflict("Trening jest już zakończony.");
        var exercise = session.Exercises.SingleOrDefault(x => x.Id == request.WorkoutExerciseId);
        if (exercise is null) return ValidationProblem("Ćwiczenie nie należy do tej sesji.");
        if (exercise.Sets.Any(x => x.SetNumber == request.SetNumber)) return Conflict("Ten numer serii jest już zapisany.");
        var set = new CompletedSet(exercise.Id, request.SetNumber, request.WeightKg, request.Repetitions, request.Rir, request.SetType);
        set.Update(request.WeightKg, request.Repetitions, request.Rir, request.SetType, request.Notes);
        exercise.Sets.Add(set);
        db.CompletedSets.Add(set);
        await db.SaveChangesAsync();
        return Created($"api/v1/workout-sessions/{id}/sets/{set.Id}", SetResponse(set));
    }

    [HttpPut("workout-sessions/{id:guid}/sets/{setId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<CompletedSetResponse>> UpdateSet(Guid id, Guid setId, SaveSetRequest request)
    {
        var session = await SessionQuery().SingleOrDefaultAsync(x => x.Id == id && x.UserId == UserId());
        if (session is null) return NotFound();
        if (session.Status != SessionStatus.InProgress) return Conflict("Trening jest już zakończony.");
        var set = session.Exercises.SelectMany(x => x.Sets).SingleOrDefault(x => x.Id == setId);
        if (set is null) return NotFound();
        set.Update(request.WeightKg, request.Repetitions, request.Rir, request.SetType, request.Notes);
        await db.SaveChangesAsync();
        return SetResponse(set);
    }

    [HttpPost("workout-sessions/{id:guid}/exercises")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<WorkoutExerciseResponse>> AddWorkoutExercise(Guid id, AddWorkoutExerciseRequest request)
    {
        var session = await SessionQuery().SingleOrDefaultAsync(x => x.Id == id && x.UserId == UserId());
        if (session is null) return NotFound();
        if (session.Status != SessionStatus.InProgress) return Conflict("Trening jest już zakończony.");
        var exercise = await db.Exercises.SingleOrDefaultAsync(x => x.Id == request.ExerciseId && x.IsActive && (x.OwnerUserId == null || x.OwnerUserId == UserId()));
        if (exercise is null || request.MinReps > request.MaxReps) return ValidationProblem("Nieprawidłowe ćwiczenie lub zakres powtórzeń.");
        var item = new WorkoutExercise(exercise, session.Exercises.Count + 1, request.PlannedSets, request.MinReps, request.MaxReps, request.TargetRir, request.RestSeconds);
        session.Exercises.Add(item);
        await db.SaveChangesAsync();
        return Created($"api/v1/workout-sessions/{id}/exercises/{item.Id}", ExerciseResponse(item));
    }

    [HttpPut("workout-sessions/{id:guid}/exercises/{workoutExerciseId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<WorkoutExerciseResponse>> ReplaceWorkoutExercise(Guid id, Guid workoutExerciseId, ReplaceWorkoutExerciseRequest request)
    {
        var session = await SessionQuery().SingleOrDefaultAsync(x => x.Id == id && x.UserId == UserId());
        if (session is null) return NotFound();
        var item = session.Exercises.SingleOrDefault(x => x.Id == workoutExerciseId);
        if (item is null) return NotFound();
        if (item.Sets.Count > 0) return Conflict("Nie można zamienić ćwiczenia po zapisaniu serii.");
        var exercise = await db.Exercises.SingleOrDefaultAsync(x => x.Id == request.ExerciseId && x.IsActive && (x.OwnerUserId == null || x.OwnerUserId == UserId()));
        if (exercise is null) return NotFound();
        item.ReplaceExercise(exercise);
        await db.SaveChangesAsync();
        return ExerciseResponse(item);
    }

    [HttpPut("workout-sessions/{id:guid}/notes")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveWorkoutNotes(Guid id, SaveWorkoutNotesRequest request)
    {
        var session = await db.WorkoutSessions.SingleOrDefaultAsync(x => x.Id == id && x.UserId == UserId());
        if (session is null) return NotFound();
        session.UpdateNotes(request.Notes);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("workout-sessions/{id:guid}/complete")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Complete(Guid id) => Finish(id, SessionStatus.Completed);

    [HttpPost("workout-sessions/{id:guid}/abandon")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Abandon(Guid id) => Finish(id, SessionStatus.Abandoned);

    [HttpGet("exercises/{id:guid}/history")]
    public async Task<IReadOnlyList<ExerciseHistoryEntry>> ExerciseHistory(Guid id)
    {
        var userId = UserId();
        return await db.CompletedSets.Where(x => db.WorkoutExercises.Any(w => w.Id == x.WorkoutExerciseId && w.ExerciseId == id && db.WorkoutSessions.Any(s => s.Id == w.WorkoutSessionId && s.UserId == userId && s.Status == SessionStatus.Completed)))
            .OrderByDescending(x => x.CompletedAtUtc).Take(50)
            .Select(x => new ExerciseHistoryEntry(x.CompletedAtUtc, x.WeightKg, x.Repetitions, x.Rir, x.WeightKg * x.Repetitions)).ToListAsync();
    }

    private async Task<IActionResult> Finish(Guid id, SessionStatus status)
    {
        var session = await db.WorkoutSessions.SingleOrDefaultAsync(x => x.Id == id && x.UserId == UserId());
        if (session is null) return NotFound();
        if (session.Status != SessionStatus.InProgress) return Conflict("Trening jest już zakończony.");
        session.Finish(status);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<TrainingPlan?> BuildPlan(string userId, SaveTrainingPlanRequest request)
    {
        var ids = request.Days.SelectMany(x => x.Exercises).Select(x => x.ExerciseId).Distinct().ToList();
        var count = await db.Exercises.CountAsync(x => ids.Contains(x.Id) && x.IsActive && (x.OwnerUserId == null || x.OwnerUserId == userId));
        if (count != ids.Count || request.Days.SelectMany(x => x.Exercises).Any(x => x.MinReps > x.MaxReps)) return null;
        var plan = new TrainingPlan(userId, request.Name, request.Goal, request.StartsOn);
        for (var i = 0; i < request.Days.Count; i++)
        {
            var source = request.Days[i];
            var day = new TrainingDay(source.Name, source.DayOfWeek, i + 1);
            for (var j = 0; j < source.Exercises.Count; j++)
            {
                var x = source.Exercises[j];
                day.Exercises.Add(new PlannedExercise(x.ExerciseId, j + 1, x.Sets, x.MinReps, x.MaxReps, x.TargetRir, x.RestSeconds));
            }
            plan.Days.Add(day);
        }
        return plan;
    }

    private async Task<IReadOnlyList<TrainingPlanResponse>> PlanResponses(IReadOnlyList<TrainingPlan> plans)
    {
        var days = plans.SelectMany(x => x.Days).ToList();
        var planned = await PlannedResponses(days.SelectMany(x => x.Exercises));
        var byId = planned.ToDictionary(x => x.Id);
        return plans.Select(plan => new TrainingPlanResponse(plan.Id, plan.Name, plan.Goal, plan.IsActive, plan.StartsOn,
            plan.Days.OrderBy(x => x.SequenceNumber).Select(day => new TrainingDayResponse(day.Id, day.Name, day.DayOfWeek, day.SequenceNumber, day.Exercises.OrderBy(x => x.Order).Select(x => byId[x.Id]).ToList())).ToList())).ToList();
    }

    private async Task<IReadOnlyList<PlannedExerciseResponse>> PlannedResponses(IEnumerable<PlannedExercise> source)
    {
        var planned = source.ToList();
        var ids = planned.Select(x => x.ExerciseId).Distinct().ToList();
        var names = await db.Exercises.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name);
        return planned.OrderBy(x => x.Order).Select(x => new PlannedExerciseResponse(x.Id, x.ExerciseId, names[x.ExerciseId], x.Sets, x.MinReps, x.MaxReps, x.TargetRir, x.RestSeconds)).ToList();
    }

    private IQueryable<WorkoutSession> SessionQuery() => db.WorkoutSessions.Include(x => x.Exercises).ThenInclude(x => x.Sets);
    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private static ExerciseResponse ExerciseResponse(Exercise x) => new(x.Id, x.Name, x.PrimaryMuscleGroup, x.Equipment, x.IsUnilateral, x.OwnerUserId != null, x.Description);
    private static CompletedSetResponse SetResponse(CompletedSet x) => new(x.Id, x.SetNumber, x.WeightKg, x.Repetitions, x.Rir, x.Type, x.CompletedAtUtc, x.Notes);
    private static WorkoutExerciseResponse ExerciseResponse(WorkoutExercise e) => new(e.Id, e.ExerciseId, e.ExerciseNameSnapshot, e.Order, e.PlannedSets, e.MinReps, e.MaxReps, e.TargetRir, e.RestSeconds, e.Sets.OrderBy(s => s.SetNumber).Select(SetResponse).ToList());
    private static WorkoutSessionResponse SessionResponse(WorkoutSession x) => new(x.Id, x.NameSnapshot, x.StartedAtUtc, x.FinishedAtUtc, x.Status,
        x.Exercises.OrderBy(e => e.Order).Select(e => new WorkoutExerciseResponse(e.Id, e.ExerciseId, e.ExerciseNameSnapshot, e.Order, e.PlannedSets, e.MinReps, e.MaxReps, e.TargetRir, e.RestSeconds, e.Sets.OrderBy(s => s.SetNumber).Select(SetResponse).ToList())).ToList());
}
