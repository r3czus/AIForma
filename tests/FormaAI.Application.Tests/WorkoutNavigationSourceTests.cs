namespace FormaAI.Application.Tests;

public sealed class WorkoutNavigationSourceTests
{
    [Fact]
    public void HomeNavigatesToDedicatedWorkoutBuilder()
    {
        var source = File.ReadAllText(SourcePath("src", "FormaAI.Web", "Pages", "Home.razor"));

        Assert.Contains("Href=\"@WorkoutEntryUrl\"", source);
        Assert.Contains("\"/workout/new\"", source);
        Assert.DoesNotContain("quick-workout-builder", source);
        Assert.DoesNotContain("ToggleQuickWorkout", source);
        Assert.DoesNotContain("_quickWorkoutOpen", source);
    }

    [Fact]
    public void WorkoutKeepsStrictSessionControlsAndAppliesAiPresets()
    {
        var source = File.ReadAllText(SourcePath("src", "FormaAI.Web", "Pages", "Workout.razor"));

        Assert.Contains("<WorkoutExerciseHero", source);
        Assert.Contains("workout-superset-strip", source);
        Assert.Contains("OnSwap=\"() => BeginSwap(exercise)\"", source);
        Assert.Contains("OnMenu=\"OpenWorkoutMenu\"", source);
        Assert.Contains("live-timer-controls", source);
        Assert.Contains("ApplyNextPreset", source);
    }

    [Fact]
    public void LiveWorkoutUsesGestureAwareExerciseHero()
    {
        var workout = File.ReadAllText(SourcePath("src", "FormaAI.Web", "Pages", "Workout.razor"));
        var heroPath = SourcePath("src", "FormaAI.Web", "Components", "Training", "WorkoutExerciseHero.razor");
        var css = File.ReadAllText(SourcePath("src", "FormaAI.Web", "wwwroot", "css", "app.css"));

        Assert.True(File.Exists(heroPath), "The gesture-aware exercise hero component must exist.");

        var hero = File.ReadAllText(heroPath);
        Assert.Contains("<WorkoutExerciseHero", workout);
        Assert.Contains("live-workout-surface", workout);
        Assert.Contains("@onpointerdown=\"HandlePointerDown\"", hero);
        Assert.Contains("@onpointerup=\"HandlePointerUp\"", hero);
        Assert.Contains("SwipeThreshold", hero);
        Assert.Contains("if (!args.IsPrimary || _pointerStartX is null || _pointerStartY is null) return;", hero);
        Assert.Contains("OnHistory=\"OpenHistory\"", workout);
        Assert.Contains("OnSwap=\"() => BeginSwap(exercise)\"", workout);
        Assert.Contains("OnMenu=\"OpenWorkoutMenu\"", workout);
        Assert.Contains("OnSelect=\"SelectExercise\"", workout);
        Assert.Contains("aria-current", hero);
        Assert.Contains("touch-action: pan-y", css);
        Assert.Contains("min-width: 44px", css);
        Assert.Contains(".workout-mode:has(.live-workout-surface)", css);
        Assert.Contains("min-height: 100dvh", css);
        Assert.Contains("overflow-x: auto", css);
        Assert.Contains("width: min(680px, calc(100% - 32px))", css);
    }

    [Fact]
    public void LiveWorkoutUsesFocusedSetRowsAndFullReplacementSheet()
    {
        var source = File.ReadAllText(SourcePath("src", "FormaAI.Web", "Pages", "Workout.razor"));

        Assert.Contains("live-set-row saved", source);
        Assert.Contains("live-set-row active", source);
        Assert.Contains("workout-sticky-action", source);
        Assert.Contains("workout-sheet workout-swap-sheet", source);
        Assert.Contains("swap-filter", source);
        Assert.Contains("aria-modal=\"true\"", source);
        Assert.Contains("@onkeydown=\"HandleSwapKeyDown\"", source);
        Assert.Contains("keyboardEvent.Key == \"Escape\"", source);
        Assert.Contains("_savingSet", source);
    }

    [Fact]
    public void LiveWorkoutUsesReferenceFaithfulFullScreenReplacementWorkflow()
    {
        var source = File.ReadAllText(SourcePath("src", "FormaAI.Web", "Pages", "Workout.razor"));
        var css = File.ReadAllText(SourcePath("src", "FormaAI.Web", "wwwroot", "css", "app.css"));

        Assert.Contains("workout-sheet workout-swap-sheet", source);
        Assert.Contains("<MudFocusTrap", source);
        Assert.Contains("DefaultFocus=\"DefaultFocus.FirstChild\"", source);
        Assert.Contains("aria-modal=\"true\"", source);
        Assert.Contains("@onkeydown=\"HandleSwapKeyDown\"", source);
        Assert.Contains("keyboardEvent.Key == \"Escape\"", source);
        Assert.Contains("swap-current-exercise", source);
        Assert.Contains("Zamień ćwiczenie", source);
        Assert.Contains("swap-search", source);
        Assert.Contains("_swapMuscleGroup", source);
        Assert.Contains("_swapEquipment", source);
        Assert.Contains("_swapSimilarOnly", source);
        Assert.Contains("SwapOptions", source);
        Assert.Contains(".Where(x => !used.Contains(x.Id))", source);
        Assert.Contains("private bool _catalogLoading = true;", source);
        Assert.Contains("@if (_catalogLoading)", source);
        Assert.Contains("_catalogLoading = false;", source);
        Assert.Contains("<ExerciseMediaFrame Exercise=\"option\"", source);
        Assert.Contains("swap-result-action", source);
        Assert.Contains("Disabled=\"@(_selectedExerciseId is null || _swapping)\"", source);
        Assert.Contains("if (_selectedExerciseId is null) return;", source);
        Assert.Contains("_swapping = true;", source);
        Assert.Contains("_swapping = false;", source);
        Assert.Contains("SwapExplanation(exercise)", source);
        Assert.Contains(".workout-swap-sheet", css);
        Assert.Contains("min-height: 100dvh", css);
        Assert.Contains(".workout-swap-sheet .swap-exercise-results", css);
        Assert.Contains("max-height: none", css);
        Assert.Contains("min-height: 0", css);
        Assert.Contains("flex: 1 1 auto", css);
        Assert.Contains(".swap-result-action", css);
        Assert.Contains("height: 48px", css);
        Assert.Contains("var(--action-soft)", css);
        Assert.Contains("safe-area-inset-bottom", css);
        Assert.Contains("body:has(.workout-swap-sheet) .bottom-nav", css);
        Assert.Contains("overflow-x: hidden", css);
    }

    [Fact]
    public void LiveWorkoutProvidesAnInPlaceFullHistorySheet()
    {
        var workout = File.ReadAllText(SourcePath("src", "FormaAI.Web", "Pages", "Workout.razor"));
        var sheetPath = SourcePath("src", "FormaAI.Web", "Components", "Training", "WorkoutHistorySheet.razor");
        var css = File.ReadAllText(SourcePath("src", "FormaAI.Web", "wwwroot", "css", "app.css"));

        Assert.True(File.Exists(sheetPath), "The full history sheet component must exist.");

        var sheet = File.ReadAllText(sheetPath);
        Assert.Contains("Dictionary<Guid, IReadOnlyList<ExerciseHistoryEntry>> _history", workout);
        Assert.Contains("_history[exerciseId] = await TrainingApi.GetHistory(exerciseId)", workout);
        Assert.Contains("history.FirstOrDefault() is { } last", workout);
        Assert.Contains("<WorkoutHistorySheet", workout);
        Assert.Contains("OnClose=\"() => _historyOpen = false\"", workout);
        Assert.DoesNotContain("NavigateTo(\"/exercise", workout);
        Assert.Contains("role=\"dialog\"", sheet);
        Assert.Contains("aria-modal=\"true\"", sheet);
        Assert.Contains("<MudFocusTrap", sheet);
        Assert.Contains("DefaultFocus=\"DefaultFocus.FirstChild\"", sheet);
        Assert.Contains("@onkeydown=\"HandleKeyDown\"", sheet);
        Assert.Contains("args.Key == \"Escape\"", sheet);
        Assert.DoesNotContain("Navigation.NavigateTo", sheet);
        Assert.DoesNotContain("_forms.Clear", workout);
        Assert.Contains("Historia", sheet);
        Assert.Contains("Wykres", sheet);
        Assert.Contains("Technika", sheet);
        Assert.Contains("TakeLast(8)", sheet);
        Assert.Contains("GroupBy(x => x.CompletedAtUtc.ToLocalTime().Date)", sheet);
        Assert.Contains(".workout-history-sheet", css);
        Assert.Contains(".workout-history-tabs", css);
        Assert.Contains(".workout-history-chart", css);
        Assert.Contains("safe-area-inset-bottom", css);
    }

    [Fact]
    public void LiveWorkoutUsesFlatSetPlateSkeletonTimersAndStickySaveAction()
    {
        var source = File.ReadAllText(SourcePath("src", "FormaAI.Web", "Pages", "Workout.razor"));
        var css = File.ReadAllText(SourcePath("src", "FormaAI.Web", "wwwroot", "css", "app.css"));

        Assert.Contains("workout-live-skeleton", source);
        Assert.Contains("live-exercise-plate", source);
        Assert.Contains("live-exercise-title", source);
        Assert.Contains("live-timer-controls", source);
        Assert.Contains("ToggleTimer", source);
        Assert.Contains("ResetTimer", source);
        Assert.Contains("CloseTimer", source);
        Assert.Contains("live-set-grid", source);
        Assert.Contains("live-set-row saved", source);
        Assert.Contains("live-set-row active", source);
        Assert.Contains("workout-sticky-action", source);
        Assert.Contains("OnClick=\"() => SaveSet(exercise, form)\"", source);
        Assert.Contains(".workout-live-skeleton", css);
        Assert.Contains(".live-exercise-plate", css);
        Assert.Contains(".live-timer-controls", css);
        Assert.Contains(".live-set-grid", css);
        Assert.Contains(".workout-sticky-action", css);
        Assert.Contains("safe-area-inset-bottom", css);
        Assert.Contains(".live-workout-surface > .session-actions", css);
        Assert.Contains("calc(128px + env(safe-area-inset-bottom))", css);
        Assert.Contains(".workout-mode:has(.workout-live-skeleton)", css);
        Assert.Contains("body:has(.workout-live-skeleton) .page-content", css);
        Assert.Contains("@keyframes workout-skeleton-sweep", css);
        Assert.Matches(@"\.live-exercise-title\s*\{\s*align-items:\s*end;", css);
    }

    [Fact]
    public void AiWorkoutReviewCanBeSavedAsCompletedOrStarted()
    {
        var source = File.ReadAllText(SourcePath("src", "FormaAI.Web", "Pages", "NewWorkout.razor"));

        Assert.Contains("Zapisz jako wykonany", source);
        Assert.Contains("SaveAiWorkoutAsCompleted", source);
        Assert.Contains("Rozpocznij ten trening", source);
        Assert.Contains("StartAiWorkout", source);
    }

    [Fact]
    public void ExerciseMediaPickerAcceptsPhotosAnimationsAndVideos()
    {
        var source = File.ReadAllText(SourcePath("src", "FormaAI.Web", "Pages", "ExerciseDetails.razor"));
        var policy = File.ReadAllText(SourcePath("src", "FormaAI.Contracts", "Training", "ExerciseMediaPolicy.cs"));

        Assert.Contains("ExerciseMediaPolicy.Accept", source);
        Assert.Contains("image/jpeg,image/png,image/webp,image/gif,video/mp4,video/webm", policy);
        Assert.Contains("Dodaj zdjęcie, GIF lub film", source);
    }

    [Fact]
    public void ExerciseMediaRenderingIsSharedAcrossTrainingSurfaces()
    {
        var component = File.ReadAllText(SourcePath("src", "FormaAI.Web", "Components", "Training", "ExerciseMediaFrame.razor"));
        var details = File.ReadAllText(SourcePath("src", "FormaAI.Web", "Pages", "ExerciseDetails.razor"));
        var training = File.ReadAllText(SourcePath("src", "FormaAI.Web", "Pages", "Training.razor"));
        var builder = File.ReadAllText(SourcePath("src", "FormaAI.Web", "Pages", "NewWorkout.razor"));
        var workout = File.ReadAllText(SourcePath("src", "FormaAI.Web", "Pages", "Workout.razor"));

        Assert.Contains("formaMotion.allowsMotion", component);
        Assert.Contains("exercise-media-frame", component);
        Assert.Contains("<ExerciseMediaFrame", details);
        Assert.Contains("ExerciseMediaPolicy.Accept", details);
        Assert.Contains("<ExerciseMediaFrame", training);
        Assert.Contains("<ExerciseMediaFrame", builder);
        Assert.Contains("<ExerciseMediaFrame", workout);
    }

    [Fact]
    public void LiveWorkoutCanCreateSupersetFromSessionExercises()
    {
        var source = File.ReadAllText(SourcePath("src", "FormaAI.Web", "Pages", "Workout.razor"));

        Assert.Contains("Połącz lub edytuj kolejność ćwiczeń", source);
        Assert.Contains("SaveSuperset", source);
        Assert.Contains("superset-builder", source);
        Assert.Contains("Liczba rund", source);
        Assert.Contains("_supersetRounds", source);
        Assert.Contains("MoveSupersetExercise", source);
        Assert.Contains("List<Guid> _supersetExerciseIds", source);
    }

    [Fact]
    public void LiveWorkoutKeepsSecondarySessionControlsInTheFocusedOptionsSheet()
    {
        var source = File.ReadAllText(SourcePath("src", "FormaAI.Web", "Pages", "Workout.razor"));
        var css = File.ReadAllText(SourcePath("src", "FormaAI.Web", "wwwroot", "css", "app.css"));

        Assert.Contains("@if (_workoutMenuOpen)", source);
        Assert.Contains("workout-sheet workout-options-sheet", source);
        Assert.Contains("<MudFocusTrap DefaultFocus=\"DefaultFocus.FirstChild\">", source);
        Assert.Contains("aria-modal=\"true\"", source);
        Assert.Contains("@onkeydown=\"HandleWorkoutMenuKeyDown\"", source);
        Assert.Contains("keyboardEvent.Key == \"Escape\"", source);
        Assert.Contains("CloseWorkoutMenu();", source);
        Assert.Contains("workout-option-row", source);
        Assert.Contains("Połącz lub edytuj kolejność ćwiczeń", source);
        Assert.Contains("Notatka i typ aktualnej serii", source);
        Assert.Contains("Dodaj ćwiczenie", source);
        Assert.Contains("Notatka do treningu", source);
        Assert.Contains("workout-session-metrics", source);
        Assert.Contains("Zakończ i zobacz podsumowanie", source);
        Assert.Contains("Porzuć trening", source);
        Assert.Contains("_workoutMenuOpen = true;", source);
        Assert.Contains("_supersetBuilderOpen = true;", source);
        Assert.Matches(@"CancelSuperset\(\);\s+CloseWorkoutMenu\(\);", source);
        Assert.DoesNotContain("swap-exercise-trigger", source);
        Assert.DoesNotContain("superset-builder-trigger", source);
        Assert.DoesNotContain("class=\"set-details\"", source);
        Assert.DoesNotContain("class=\"exercise-page-actions\"", source);
        Assert.DoesNotContain("class=\"workout-options\"", source);
        Assert.DoesNotContain("class=\"session-actions\"", source);
        Assert.Contains("workout-superset-strip", source);
        Assert.Contains("<ExerciseMediaFrame Exercise=\"@SupersetExerciseMedia(member)\"", source);
        Assert.Contains(".workout-options-sheet", css);
        Assert.Contains(".workout-options-content", css);
        Assert.Contains(".workout-option-row", css);
        Assert.Contains("min-height: 56px", css);
        Assert.Contains(".workout-option-details", css);
        Assert.Contains(".workout-session-metrics", css);
        Assert.Contains(".workout-options-sheet .superset-builder", css);
        Assert.Contains("safe-area-inset-bottom", css);
    }

    [Fact]
    public void TrainingModuleUsesThreeStableTabs()
    {
        var source = File.ReadAllText(SourcePath("src", "FormaAI.Web", "Pages", "Training.razor"));

        Assert.Contains("<MudTabPanel Text=\"Trening\">", source);
        Assert.Contains("<MudTabPanel Text=\"Plany\">", source);
        Assert.Contains("<MudTabPanel Text=\"Ćwiczenia\">", source);
        Assert.DoesNotContain("<MudTabPanel Text=\"Nowy plan\">", source);
        Assert.DoesNotContain("<MudTabPanel Text=\"Nowe ćwiczenie\">", source);
    }

    [Fact]
    public void SavedMealClickableCopyIsLeftAligned()
    {
        var source = File.ReadAllText(SourcePath("src", "FormaAI.Web", "wwwroot", "css", "app.css"));

        Assert.Contains(".meal-row-link", source);
        Assert.Contains("text-align: left", source);
    }

    [Fact]
    public void CardioOnlyLiveWorkoutCanBeFinished()
    {
        var source = File.ReadAllText(SourcePath("src", "FormaAI.Web", "Pages", "Workout.razor"));

        Assert.Contains("workout-cardio-summary", source);
        Assert.Contains("Zakończ trening cardio", source);
    }

    private static string SourcePath(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "FormaAI.sln")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        return Path.Combine([directory.FullName, .. parts]);
    }
}
