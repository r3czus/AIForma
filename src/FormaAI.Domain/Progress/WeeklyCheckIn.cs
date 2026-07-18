namespace FormaAI.Domain.Progress;

public sealed class WeeklyCheckIn
{
    private WeeklyCheckIn() { }

    public WeeklyCheckIn(string userId, DateOnly localDate, int energy, int sleep, int hunger, int recovery, string? notes)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        LocalDate = localDate;
        Energy = energy;
        Sleep = sleep;
        Hunger = hunger;
        Recovery = recovery;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = null!;
    public DateOnly LocalDate { get; private set; }
    public int Energy { get; private set; }
    public int Sleep { get; private set; }
    public int Hunger { get; private set; }
    public int Recovery { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
