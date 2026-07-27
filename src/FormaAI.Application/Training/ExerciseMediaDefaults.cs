namespace FormaAI.Application.Training;

public sealed record ExerciseMediaDefault(string Url, string ContentType, string Attribution);

public static class ExerciseMediaDefaults
{
    private static readonly IReadOnlyDictionary<string, ExerciseMediaDefault> Defaults =
        new Dictionary<string, ExerciseMediaDefault>(StringComparer.OrdinalIgnoreCase)
        {
            ["Wyciskanie sztangi leżąc"] = new(
                "/images/exercises/wyciskanie-sztangi-lezac.png",
                "image/png",
                "FormaAI · obraz wygenerowany"),
            ["Wiosłowanie na maszynie z podparciem"] = new(
                "/images/exercises/wioslowanie-maszyna-podparcie.png",
                "image/png",
                "FormaAI · obraz wygenerowany")
        };

    public static ExerciseMediaDefault? Resolve(string exerciseName) =>
        Defaults.GetValueOrDefault(exerciseName);
}
