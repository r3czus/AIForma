namespace FormaAI.Contracts.Training;

public static class ExerciseMediaPolicy
{
    public const long MaxBytes = 15 * 1024 * 1024;
    public const string Accept = "image/jpeg,image/png,image/webp,image/gif,video/mp4,video/webm";
    public const string ValidationMessage = "Wybierz JPG, PNG, WebP, GIF, MP4 albo WebM do 15 MB.";

    private static readonly Dictionary<string, string[]> Extensions =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = [".jpg", ".jpeg"],
            ["image/png"] = [".png"],
            ["image/webp"] = [".webp"],
            ["image/gif"] = [".gif"],
            ["video/mp4"] = [".mp4"],
            ["video/webm"] = [".webm"]
        };

    public static bool TryNormalize(
        string? contentType,
        string? fileName,
        out string normalizedType,
        out string normalizedExtension)
    {
        normalizedType = string.Empty;
        normalizedExtension = string.Empty;

        if (string.IsNullOrWhiteSpace(contentType) || string.IsNullOrWhiteSpace(fileName))
            return false;

        var type = contentType.Trim().ToLowerInvariant();
        var sourceExtension = Path.GetExtension(fileName);
        if (!Extensions.TryGetValue(type, out var allowedExtensions) ||
            !allowedExtensions.Contains(sourceExtension, StringComparer.OrdinalIgnoreCase))
            return false;

        normalizedType = type;
        normalizedExtension = type == "image/jpeg" ? ".jpg" : allowedExtensions[0];
        return true;
    }
}
