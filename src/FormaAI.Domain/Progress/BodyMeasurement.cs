namespace FormaAI.Domain.Progress;

public sealed class BodyMeasurement
{
    private BodyMeasurement() { }

    public BodyMeasurement(string userId, DateOnly localDate, decimal weightKg, decimal? waistCm, string? notes)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        MeasuredAtUtc = DateTime.UtcNow;
        LocalDate = localDate;
        WeightKg = weightKg;
        WaistCm = waistCm;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public DateTime MeasuredAtUtc { get; private set; }
    public DateOnly LocalDate { get; private set; }
    public decimal WeightKg { get; private set; }
    public decimal? WaistCm { get; private set; }
    public string? Notes { get; private set; }
}
