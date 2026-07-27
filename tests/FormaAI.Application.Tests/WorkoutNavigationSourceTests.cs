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

        Assert.Contains("workout-motion-hero", source);
        Assert.Contains("workout-superset-strip", source);
        Assert.Contains("swap-exercise-trigger", source);
        Assert.Contains("exercise-timer-actions", source);
        Assert.Contains("ApplyNextPreset", source);
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

        Assert.Contains("image/jpeg,image/png,image/webp,image/gif,video/mp4,video/webm", source);
        Assert.Contains("Dodaj zdjęcie, GIF lub film", source);
    }

    [Fact]
    public void LiveWorkoutCanCreateSupersetFromSessionExercises()
    {
        var source = File.ReadAllText(SourcePath("src", "FormaAI.Web", "Pages", "Workout.razor"));

        Assert.Contains("Połącz w superserię", source);
        Assert.Contains("SaveSuperset", source);
        Assert.Contains("superset-builder", source);
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
