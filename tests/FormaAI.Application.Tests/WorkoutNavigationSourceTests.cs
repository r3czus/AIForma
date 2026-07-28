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
        Assert.Contains("swap-exercise-trigger", source);
        Assert.Contains("exercise-timer-actions", source);
        Assert.Contains("ApplyNextPreset", source);
    }

    [Fact]
    public void LiveWorkoutUsesGestureAwareExerciseHero()
    {
        var workout = File.ReadAllText(SourcePath("src", "FormaAI.Web", "Pages", "Workout.razor"));
        var heroPath = SourcePath("src", "FormaAI.Web", "Components", "Training", "WorkoutExerciseHero.razor");

        Assert.True(File.Exists(heroPath), "The gesture-aware exercise hero component must exist.");

        var hero = File.ReadAllText(heroPath);
        Assert.Contains("<WorkoutExerciseHero", workout);
        Assert.Contains("live-workout-surface", workout);
        Assert.Contains("@onpointerdown=\"HandlePointerDown\"", hero);
        Assert.Contains("@onpointerup=\"HandlePointerUp\"", hero);
        Assert.Contains("SwipeThreshold", hero);
        Assert.Contains("if (!args.IsPrimary || _pointerStartX is null || _pointerStartY is null) return;", hero);
    }

    [Fact]
    public void LiveWorkoutUsesFocusedSetRowsAndFullReplacementSheet()
    {
        var source = File.ReadAllText(SourcePath("src", "FormaAI.Web", "Pages", "Workout.razor"));

        Assert.Contains("workout-set-row saved", source);
        Assert.Contains("workout-set-row active", source);
        Assert.Contains("workout-primary-action", source);
        Assert.Contains("workout-sheet swap-sheet", source);
        Assert.Contains("swap-filter", source);
        Assert.Contains("aria-modal=\"true\"", source);
        Assert.Contains("@onkeydown=\"HandleSwapKeyDown\"", source);
        Assert.Contains("keyboardEvent.Key == \"Escape\"", source);
        Assert.Contains("_savingSet", source);
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

        Assert.Contains("Połącz w superserię", source);
        Assert.Contains("SaveSuperset", source);
        Assert.Contains("superset-builder", source);
        Assert.Contains("Liczba rund", source);
        Assert.Contains("_supersetRounds", source);
        Assert.Contains("MoveSupersetExercise", source);
        Assert.Contains("List<Guid> _supersetExerciseIds", source);
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
