namespace FormaAI.Application.Nutrition;

public sealed record MealImageDescriptor(string ContentType, long Length);

public static class MealImageBatchValidator
{
    private const long MaximumBytes = 12 * 1024 * 1024;
    private static readonly HashSet<string> SupportedTypes =
    [
        "image/jpeg", "image/png", "image/webp", "image/heic", "image/heif"
    ];

    public static string? Validate(IReadOnlyList<MealImageDescriptor> images)
    {
        ArgumentNullException.ThrowIfNull(images);
        if (images.Count is < 1 or > 5) return "Wybierz od 1 do 5 zdjęć.";

        var invalidSize = images.Select((image, index) => (image, index))
            .FirstOrDefault(x => x.image.Length is <= 0 or > MaximumBytes);
        if (invalidSize.image is not null)
            return $"Zdjęcie {invalidSize.index + 1} może mieć maksymalnie 12 MB.";

        var invalidType = images.Select((image, index) => (image, index))
            .FirstOrDefault(x => !SupportedTypes.Contains(x.image.ContentType.ToLowerInvariant()));
        return invalidType.image is null
            ? null
            : $"Zdjęcie {invalidType.index + 1} ma nieobsługiwany format. Wybierz JPEG, PNG, WEBP, HEIC lub HEIF.";
    }
}
