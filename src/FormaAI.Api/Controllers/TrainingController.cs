using System.Security.Claims;
using FormaAI.Application.Common;
using FormaAI.Application.Training;
using FormaAI.Contracts.Training;
using FormaAI.Domain.Training;
using FormaAI.Domain.Progress;
using FormaAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormaAI.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class TrainingController(AppDbContext db, IWebHostEnvironment environment, IConfiguration configuration) : ControllerBase
{
    [HttpGet("exercises")]
    public async Task<IReadOnlyList<ExerciseResponse>> Exercises([FromQuery] string? query)
    {
        var userId = UserId();
        var exercises = db.Exercises.Include(x => x.MuscleEngagements).Where(x => x.IsActive && (x.OwnerUserId == null || x.OwnerUserId == userId));
        var search = WildcardSearch.Parse(query);
        if (!string.IsNullOrWhiteSpace(search.Value))
        {
            exercises = search.Mode switch
            {
                WildcardSearchMode.StartsWith => exercises.Where(x => x.Name.StartsWith(search.Value)),
                WildcardSearchMode.EndsWith => exercises.Where(x => x.Name.EndsWith(search.Value)),
                _ => exercises.Where(x => x.Name.Contains(search.Value))
            };
        }
        return (await exercises.OrderBy(x => x.Name).Take(50).ToListAsync()).Select(ExerciseResponse).ToList();
    }

    [HttpGet("exercises/{id:guid}")]
    public async Task<ActionResult<ExerciseResponse>> Exercise(Guid id)
    {
        var userId = UserId();
        var exercise = await db.Exercises.Include(x => x.MuscleEngagements)
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive && (x.OwnerUserId == null || x.OwnerUserId == userId));
        if (exercise is null) return NotFound();
        return ExerciseResponse(exercise);
    }

    [HttpPost("exercises")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<ExerciseResponse>> CreateExercise(SaveExerciseRequest request)
    {
        if (!ValidEngagements(request)) return ValidationProblem("Wybierz 1–5 unikalnych partii, których suma wynosi 100%.");
        var exercise = new Exercise(UserId(), request.Name, request.MuscleGroup, request.Equipment, request.IsUnilateral, request.Description);
        exercise.SetMuscleEngagements(Engagements(request));
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();
        return Created($"api/v1/exercises/{exercise.Id}", ExerciseResponse(exercise));
    }

    [HttpPut("exercises/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<ExerciseResponse>> UpdateExercise(Guid id, SaveExerciseRequest request)
    {
        if (!ValidEngagements(request)) return ValidationProblem("Wybierz 1–5 unikalnych partii, których suma wynosi 100%.");
        var exercise = await db.Exercises.Include(x => x.MuscleEngagements).SingleOrDefaultAsync(x => x.Id == id && x.OwnerUserId == UserId());
        if (exercise is null) return NotFound();
        exercise.Update(request.Name, request.MuscleGroup, request.Equipment, request.IsUnilateral, request.Description);
        exercise.SetMuscleEngagements(Engagements(request));
        await db.SaveChangesAsync();
        return ExerciseResponse(exercise);
    }

    [HttpPost("exercises/{id:guid}/media")]
    [ValidateAntiForgeryToken]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(16 * 1024 * 1024)]
    public async Task<ActionResult<ExerciseResponse>> UploadExerciseMedia(
        Guid id,
        [FromForm] IFormFile media,
        [FromForm] string author,
        [FromForm] string license,
        [FromForm] string? sourceUrl,
        CancellationToken cancellationToken)
    {
        var exercise = await db.Exercises.Include(x => x.MuscleEngagements).SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        if (exercise is null || !CanEditMedia(exercise)) return NotFound();
        var contentType = media.ContentType.ToLowerInvariant();
        if (media.Length is 0 or > 15 * 1024 * 1024 ||
            contentType is not ("image/jpeg" or "image/png" or "image/webp" or "image/gif" or "video/mp4" or "video/webm"))
            return BadRequest("Wybierz JPG, PNG, WebP, GIF, MP4 albo WebM do 15 MB.");
        if (string.IsNullOrWhiteSpace(author) || author.Length > 150 || string.IsNullOrWhiteSpace(license) || license.Length > 100)
            return BadRequest("Podaj autora i licencję materiału.");
        if (!string.IsNullOrWhiteSpace(sourceUrl) &&
            (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var source) || source.Scheme is not ("http" or "https")))
            return BadRequest("Link do źródła musi być poprawnym adresem HTTP lub HTTPS.");

        var storage = Path.Combine(environment.ContentRootPath, "App_Data", "exercise-media");
        Directory.CreateDirectory(storage);
        var extension = contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            "video/mp4" => ".mp4",
            _ => ".webm"
        };
        var storageName = $"{Guid.NewGuid():N}{extension}";
        await using (var stream = System.IO.File.Create(Path.Combine(storage, storageName)))
            await media.CopyToAsync(stream, cancellationToken);

        var previousStorageName = exercise.MediaStorageName;
        exercise.SetMedia(storageName, contentType, $"{author.Trim()} · {license.Trim()}", sourceUrl);
        await db.SaveChangesAsync(cancellationToken);
        DeleteStoredMedia(storage, previousStorageName);
        return ExerciseResponse(exercise);
    }

    [HttpGet("exercises/{id:guid}/media/content")]
    public async Task<IActionResult> ExerciseMedia(Guid id, CancellationToken cancellationToken)
    {
        var userId = UserId();
        var exercise = await db.Exercises.SingleOrDefaultAsync(x => x.Id == id && x.IsActive &&
            (x.OwnerUserId == null || x.OwnerUserId == userId), cancellationToken);
        if (exercise?.MediaStorageName is null || exercise.MediaContentType is null) return NotFound();
        var path = Path.Combine(environment.ContentRootPath, "App_Data", "exercise-media", exercise.MediaStorageName);
        return System.IO.File.Exists(path) ? PhysicalFile(path, exercise.MediaContentType, enableRangeProcessing: true) : NotFound();
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
        var localDate = DateOnly.FromDateTime(localNow);
        var exception = await db.TrainingScheduleExceptions.Where(x => x.UserId == userId && (x.NewDate == localDate || x.OriginalDate == localDate)).OrderByDescending(x => x.NewDate == localDate).FirstOrDefaultAsync();
        var day = exception?.NewDate == localDate ? plan.Days.FirstOrDefault(x => x.Id == exception.TrainingDayId)
            : exception is { OriginalDate: var original } && original == localDate && exception.Decision != ScheduleDecision.Completed ? null
            : plan.Days.FirstOrDefault(x => x.DayOfWeek == localNow.DayOfWeek)
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
        var catalog = await db.Exercises.Include(x => x.MuscleEngagements).Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        var session = new WorkoutSession(userId, plan, day);
        var plannedExercises = day.Exercises.OrderBy(x => x.Order).ToList();
        if (request.TimeLimitMinutes is 15 or 30)
        {
            session.MarkShortened(request.TimeLimitMinutes.Value);
            plannedExercises = plannedExercises.Take(request.TimeLimitMinutes == 15 ? 3 : 4).ToList();
        }
        foreach (var planned in plannedExercises)
        {
            var item = new WorkoutExercise(planned, catalog[planned.ExerciseId]);
            if (request.TimeLimitMinutes is 15) item.Shorten(2);
            else if (request.TimeLimitMinutes is 30) item.Shorten(3);
            session.Exercises.Add(item);
        }
        db.WorkoutSessions.Add(session);
        await db.SaveChangesAsync();
        return Created($"api/v1/workout-sessions/{session.Id}", SessionResponse(session));
    }

    [HttpPost("workout-sessions/quick")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<WorkoutSessionResponse>> StartQuick(StartQuickWorkoutRequest request)
    {
        var userId = UserId();
        var active = await SessionQuery().OrderByDescending(x => x.StartedAtUtc)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Status == SessionStatus.InProgress);
        if (active is not null) return Conflict("Najpierw dokończ albo porzuć aktywny trening.");

        var requestedIds = request.Exercises.Select(x => x.ExerciseId).ToList();
        if (requestedIds.Distinct().Count() != requestedIds.Count)
            return ValidationProblem("Każde ćwiczenie można dodać do szybkiego treningu tylko raz.");
        if (!ValidQuickWorkout(request.Exercises))
            return ValidationProblem("Sprawdź zakres powtórzeń, presety serii i konfigurację superserii.");

        var catalog = await db.Exercises.Include(x => x.MuscleEngagements)
            .Where(x => requestedIds.Contains(x.Id) && x.IsActive && (x.OwnerUserId == null || x.OwnerUserId == userId))
            .ToDictionaryAsync(x => x.Id);
        if (catalog.Count != requestedIds.Count)
            return ValidationProblem("Lista zawiera niedostępne ćwiczenie.");

        var session = new WorkoutSession(userId, request.Name, request.TimeLimitMinutes);
        for (var index = 0; index < request.Exercises.Count; index++)
        {
            var selected = request.Exercises[index];
            var workoutExercise = new WorkoutExercise(
                catalog[selected.ExerciseId],
                index + 1,
                selected.Sets,
                selected.MinReps,
                selected.MaxReps,
                selected.TargetRir,
                selected.RestSeconds,
                selected.SupersetGroupId,
                selected.SupersetPosition,
                selected.IntervalSeconds);
            foreach (var preset in selected.Presets ?? [])
            {
                workoutExercise.Presets.Add(new WorkoutSetPreset(
                    workoutExercise.Id,
                    preset.SetNumber,
                    preset.WeightKg,
                    preset.Repetitions,
                    preset.Rir));
            }
            session.Exercises.Add(workoutExercise);
        }

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
        var exercise = await db.Exercises.Include(x => x.MuscleEngagements).SingleOrDefaultAsync(x => x.Id == request.ExerciseId && x.IsActive && (x.OwnerUserId == null || x.OwnerUserId == UserId()));
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
        if (session.Status != SessionStatus.InProgress) return Conflict("Trening jest już zakończony.");
        var item = session.Exercises.SingleOrDefault(x => x.Id == workoutExerciseId);
        if (item is null) return NotFound();
        if (session.Exercises.Any(x => x.Id != item.Id && x.ExerciseId == request.ExerciseId))
            return Conflict("To ćwiczenie jest już w tej sesji.");
        var exercise = await db.Exercises.Include(x => x.MuscleEngagements).SingleOrDefaultAsync(x => x.Id == request.ExerciseId && x.IsActive && (x.OwnerUserId == null || x.OwnerUserId == UserId()));
        if (exercise is null) return NotFound();

        if (item.Sets.Count == 0)
        {
            item.ReplaceExercise(exercise);
            await db.SaveChangesAsync();
            return ExerciseResponse(item);
        }

        foreach (var later in session.Exercises.Where(x => x.Order > item.Order))
            later.ChangeOrder(later.Order + 1);
        var remainingSets = Math.Max(1, item.PlannedSets - item.Sets.Count);
        var supersetGroupId = item.SupersetGroupId;
        var supersetPosition = item.SupersetPosition;
        var intervalSeconds = item.IntervalSeconds;
        item.Shorten(item.Sets.Count);
        item.DetachFromSuperset();
        var replacement = new WorkoutExercise(
            exercise,
            item.Order + 1,
            remainingSets,
            item.MinReps,
            item.MaxReps,
            item.TargetRir,
            item.RestSeconds,
            supersetGroupId,
            supersetPosition,
            intervalSeconds);
        session.Exercises.Add(replacement);
        db.WorkoutExercises.Add(replacement);
        await db.SaveChangesAsync();
        return ExerciseResponse(replacement);
    }

    [HttpPut("workout-sessions/{id:guid}/superset")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<WorkoutSessionResponse>> UpdateWorkoutSuperset(
        Guid id,
        UpdateWorkoutSupersetRequest request)
    {
        var session = await SessionQuery().SingleOrDefaultAsync(x => x.Id == id && x.UserId == UserId());
        if (session is null) return NotFound();
        if (session.Status != SessionStatus.InProgress) return Conflict("Trening jest już zakończony.");
        if (request.WorkoutExerciseIds.Count is < 2 or > 5 ||
            request.WorkoutExerciseIds.Distinct().Count() != request.WorkoutExerciseIds.Count)
            return ValidationProblem("Superseria musi zawierać od 2 do 5 różnych ćwiczeń.");

        var selected = request.WorkoutExerciseIds
            .Select(exerciseId => session.Exercises.SingleOrDefault(x => x.Id == exerciseId))
            .ToList();
        if (selected.Any(x => x is null))
            return ValidationProblem("Wybrane ćwiczenie nie należy do tej sesji.");

        var previousGroups = selected
            .Where(x => x!.SupersetGroupId is not null)
            .Select(x => x!.SupersetGroupId!.Value)
            .ToHashSet();
        foreach (var exercise in session.Exercises.Where(x => x.SupersetGroupId is Guid groupId && previousGroups.Contains(groupId)))
            exercise.DetachFromSuperset();

        var supersetId = Guid.NewGuid();
        for (var index = 0; index < selected.Count; index++)
            selected[index]!.ConfigureSuperset(supersetId, index + 1, request.IntervalSeconds, request.RestSeconds);

        await db.SaveChangesAsync();
        return SessionResponse(session);
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

    [HttpGet("workout-sessions/{id:guid}/progressions")]
    public async Task<IReadOnlyList<ExerciseProgressionResponse>> Progressions(Guid id)
    {
        var userId = UserId();
        var rows = await db.ExerciseProgressions.Where(x => x.UserId == userId && x.SourceSessionId == id).ToListAsync();
        var names = await db.Exercises.Where(x => rows.Select(r => r.ExerciseId).Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name);
        return rows.Select(x => new ExerciseProgressionResponse(x.Id, x.ExerciseId, names.GetValueOrDefault(x.ExerciseId, "Ćwiczenie"), x.SuggestedWeightKg, x.MinReps, x.MaxReps, x.Reason, x.Decision, x.AcceptedWeightKg)).ToList();
    }

    [HttpPut("exercise-progressions/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<ExerciseProgressionResponse>> DecideProgression(Guid id, DecideProgressionRequest request)
    {
        var item = await db.ExerciseProgressions.SingleOrDefaultAsync(x => x.Id == id && x.UserId == UserId());
        if (item is null) return NotFound();
        item.Decide(request.Decision, request.WeightKg ?? item.SuggestedWeightKg);
        await db.SaveChangesAsync();
        var name = await db.Exercises.Where(x => x.Id == item.ExerciseId).Select(x => x.Name).SingleAsync();
        return new ExerciseProgressionResponse(item.Id, item.ExerciseId, name, item.SuggestedWeightKg, item.MinReps, item.MaxReps, item.Reason, item.Decision, item.AcceptedWeightKg);
    }

    [HttpGet("training-schedule")]
    public async Task<IReadOnlyList<TrainingScheduleExceptionResponse>> TrainingSchedule([FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        var userId = UserId();
        var rows = await db.TrainingScheduleExceptions.Where(x => x.UserId == userId && x.OriginalDate <= to && (x.NewDate ?? x.OriginalDate) >= from).ToListAsync();
        var names = await db.TrainingDays.Where(x => rows.Select(r => r.TrainingDayId).Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name);
        return rows.Select(x => new TrainingScheduleExceptionResponse(x.Id, x.TrainingDayId, names.GetValueOrDefault(x.TrainingDayId, "Trening"), x.OriginalDate, x.NewDate, x.Decision, x.Reason)).ToList();
    }

    [HttpPost("training-schedule")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<TrainingScheduleExceptionResponse>> SaveTrainingSchedule(SaveTrainingScheduleExceptionRequest request)
    {
        var userId = UserId();
        var day = await db.TrainingDays.SingleOrDefaultAsync(x => x.Id == request.TrainingDayId && db.TrainingPlans.Any(p => p.Id == x.TrainingPlanId && p.UserId == userId));
        if (day is null) return NotFound();
        var old = await db.TrainingScheduleExceptions.SingleOrDefaultAsync(x => x.UserId == userId && x.TrainingDayId == request.TrainingDayId && x.OriginalDate == request.OriginalDate);
        if (old is not null) db.TrainingScheduleExceptions.Remove(old);
        var item = new TrainingScheduleException(userId, request.TrainingDayId, request.OriginalDate, request.NewDate, request.Decision, request.Reason);
        db.TrainingScheduleExceptions.Add(item);
        await db.SaveChangesAsync();
        return new TrainingScheduleExceptionResponse(item.Id, item.TrainingDayId, day.Name, item.OriginalDate, item.NewDate, item.Decision, item.Reason);
    }

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
        var session = await SessionQuery().SingleOrDefaultAsync(x => x.Id == id && x.UserId == UserId());
        if (session is null) return NotFound();
        if (session.Status != SessionStatus.InProgress) return Conflict("Trening jest już zakończony.");
        session.Finish(status);
        if (status == SessionStatus.Completed)
        {
            foreach (var exercise in session.Exercises.Where(x => x.ExerciseId.HasValue))
            {
                var sets = exercise.Sets.Where(x => x.Type == SetType.Working).ToList();
                if (sets.Count == 0) continue;
                var top = sets.Max(x => x.WeightKg);
                var reachedTop = sets.Count >= exercise.PlannedSets && sets.All(x => x.Repetitions >= exercise.MaxReps) && sets.All(x => !x.Rir.HasValue || x.Rir >= (exercise.TargetRir ?? 1));
                var suggested = reachedTop ? Math.Ceiling((top + 2.5m) / 2.5m) * 2.5m : top;
                var reason = reachedTop ? "Wszystkie serie osiągnęły górny zakres z zapasem."
                    : sets.Any(x => x.Repetitions < exercise.MinReps) ? "Najpierw ustabilizuj dolny zakres powtórzeń." : "Dodaj powtórzenie, zanim zwiększysz ciężar.";
                db.ExerciseProgressions.Add(new ExerciseProgression(session.UserId, exercise.ExerciseId!.Value, session.Id, suggested, exercise.MinReps, exercise.MaxReps, reason));
            }
        }
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<TrainingPlan?> BuildPlan(string userId, SaveTrainingPlanRequest request)
    {
        var ids = request.Days.SelectMany(x => x.Exercises).Select(x => x.ExerciseId).Distinct().ToList();
        var count = await db.Exercises.CountAsync(x => ids.Contains(x.Id) && x.IsActive && (x.OwnerUserId == null || x.OwnerUserId == userId));
        if (count != ids.Count ||
            request.Days.SelectMany(x => x.Exercises).Any(x => x.MinReps > x.MaxReps) ||
            request.Days.Any(x => !ValidSupersets(x.Exercises)))
            return null;
        var plan = new TrainingPlan(userId, request.Name, request.Goal, request.StartsOn);
        for (var i = 0; i < request.Days.Count; i++)
        {
            var source = request.Days[i];
            var day = new TrainingDay(source.Name, source.DayOfWeek, i + 1);
            for (var j = 0; j < source.Exercises.Count; j++)
            {
                var x = source.Exercises[j];
                day.Exercises.Add(new PlannedExercise(
                    x.ExerciseId,
                    j + 1,
                    x.Sets,
                    x.MinReps,
                    x.MaxReps,
                    x.TargetRir,
                    x.RestSeconds,
                    x.SupersetGroupId,
                    x.SupersetPosition,
                    x.IntervalSeconds));
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
        return planned.OrderBy(x => x.Order).Select(x => new PlannedExerciseResponse(
            x.Id,
            x.ExerciseId,
            names[x.ExerciseId],
            x.Sets,
            x.MinReps,
            x.MaxReps,
            x.TargetRir,
            x.RestSeconds,
            x.SupersetGroupId,
            x.SupersetPosition,
            x.IntervalSeconds)).ToList();
    }

    private IQueryable<WorkoutSession> SessionQuery() => db.WorkoutSessions
        .Include(x => x.Exercises).ThenInclude(x => x.Sets)
        .Include(x => x.Exercises).ThenInclude(x => x.Presets)
        .Include(x => x.Exercises).ThenInclude(x => x.MuscleEngagements);
    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private static bool ValidEngagements(SaveExerciseRequest request)
    {
        var rows = request.MuscleEngagements;
        return rows is null || rows.Count == 0 || rows.Count is >= 1 and <= 5 && rows.All(x => x.Percentage is >= 1 and <= 100)
            && rows.Sum(x => x.Percentage) == 100 && rows.Select(x => x.MuscleGroup).Distinct().Count() == rows.Count;
    }
    private static IEnumerable<(MuscleGroup Group, int Percentage)> Engagements(SaveExerciseRequest request) =>
        request.MuscleEngagements is { Count: > 0 } ? request.MuscleEngagements.Select(x => (x.MuscleGroup, x.Percentage)) : [(request.MuscleGroup, 100)];
    private static bool ValidSupersets(IReadOnlyList<PlannedExerciseRequest> exercises)
    {
        if (exercises.Any(x => x.SupersetGroupId is null && (x.SupersetPosition is not null || x.IntervalSeconds is not null)))
            return false;

        return exercises
            .Where(x => x.SupersetGroupId is not null)
            .GroupBy(x => x.SupersetGroupId)
            .All(group =>
            {
                var positions = group.Select(x => x.SupersetPosition).ToList();
                return group.Count() >= 2 &&
                       positions.All(x => x is > 0) &&
                       positions.Distinct().Count() == positions.Count &&
                       positions.OrderBy(x => x).SequenceEqual(Enumerable.Range(1, positions.Count).Select(x => (int?)x));
            });
    }
    private static bool ValidQuickWorkout(IReadOnlyList<QuickWorkoutExerciseRequest> exercises)
    {
        if (exercises.Any(x =>
                x.MinReps > x.MaxReps ||
                x.Presets is { Count: > 0 } &&
                (x.Presets.Count > x.Sets ||
                 x.Presets.Select(p => p.SetNumber).Distinct().Count() != x.Presets.Count ||
                 x.Presets.Any(p => p.SetNumber > x.Sets))))
            return false;
        if (exercises.Any(x => x.SupersetGroupId is null && (x.SupersetPosition is not null || x.IntervalSeconds is not null)))
            return false;

        return exercises
            .Where(x => x.SupersetGroupId is not null)
            .GroupBy(x => x.SupersetGroupId)
            .All(group =>
            {
                var positions = group.Select(x => x.SupersetPosition).ToList();
                return group.Count() >= 2 &&
                       positions.All(x => x is > 0) &&
                       positions.Distinct().Count() == positions.Count &&
                       positions.OrderBy(x => x).SequenceEqual(Enumerable.Range(1, positions.Count).Select(x => (int?)x));
            });
    }
    private ExerciseResponse ExerciseResponse(Exercise x)
    {
        var mediaDefault = x.MediaStorageName is null && x.MediaExternalUrl is null
            ? ExerciseMediaDefaults.Resolve(x.Name)
            : null;
        return new ExerciseResponse(
            x.Id,
            x.Name,
            x.PrimaryMuscleGroup,
            x.Equipment,
            x.IsUnilateral,
            x.OwnerUserId != null,
            x.Description,
            x.MuscleEngagements.Count > 0
                ? x.MuscleEngagements.OrderByDescending(e => e.Percentage)
                    .Select(e => new ExerciseMuscleEngagementResponse(e.MuscleGroup, e.Percentage)).ToList()
                : [new(x.PrimaryMuscleGroup, 100)],
            x.MediaStorageName is not null
                ? $"api/v1/exercises/{x.Id}/media/content"
                : x.MediaExternalUrl ?? mediaDefault?.Url,
            x.MediaContentType ?? mediaDefault?.ContentType,
            x.MediaAttribution ?? mediaDefault?.Attribution,
            x.MediaSourceUrl,
            CanEditMedia(x));
    }
    private bool CanEditMedia(Exercise exercise) => exercise.OwnerUserId == UserId() ||
        exercise.OwnerUserId is null && string.Equals(User.Identity?.Name, configuration["Admin:Email"], StringComparison.OrdinalIgnoreCase);
    private static void DeleteStoredMedia(string storage, string? storageName)
    {
        if (string.IsNullOrWhiteSpace(storageName)) return;
        var path = Path.Combine(storage, storageName);
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
    }
    private static CompletedSetResponse SetResponse(CompletedSet x) => new(x.Id, x.SetNumber, x.WeightKg, x.Repetitions, x.Rir, x.Type, x.CompletedAtUtc, x.Notes);
    private static WorkoutExerciseResponse ExerciseResponse(WorkoutExercise e) => new(
        e.Id,
        e.ExerciseId,
        e.ExerciseNameSnapshot,
        e.Order,
        e.PlannedSets,
        e.MinReps,
        e.MaxReps,
        e.TargetRir,
        e.RestSeconds,
        e.Sets.OrderBy(s => s.SetNumber).Select(SetResponse).ToList(),
        e.SupersetGroupId,
        e.SupersetPosition,
        e.IntervalSeconds,
        e.Presets.OrderBy(p => p.SetNumber)
            .Select(p => new WorkoutSetPresetResponse(p.SetNumber, p.WeightKg, p.Repetitions, p.Rir))
            .ToList());
    private static WorkoutSessionResponse SessionResponse(WorkoutSession x) => new(x.Id, x.NameSnapshot, x.StartedAtUtc, x.FinishedAtUtc, x.Status,
        x.Exercises.OrderBy(e => e.Order).Select(ExerciseResponse).ToList(), x.IsShortened, x.TimeLimitMinutes);
}
