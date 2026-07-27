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

    private static string SourcePath(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "FormaAI.sln")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        return Path.Combine([directory.FullName, .. parts]);
    }
}
