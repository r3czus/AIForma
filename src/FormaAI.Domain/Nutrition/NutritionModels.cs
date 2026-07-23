namespace FormaAI.Domain.Nutrition;

public enum ServingUnit { Gram, Milliliter, Piece }
public enum ProductSource { Manual, OpenFoodFacts, System }
public enum MealSource { Manual, AssistantDraft, Template }
public enum NutritionDayStatus { Unknown, Complete, Partial, NoData }

public sealed class NutritionTarget
{
    private NutritionTarget() { }

    public NutritionTarget(string userId, DateOnly effectiveFrom, decimal calories, decimal protein, decimal fat, decimal carbs)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        EffectiveFrom = effectiveFrom;
        CaloriesKcal = calories;
        ProteinG = protein;
        FatG = fat;
        CarbohydratesG = carbs;
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = null!;
    public DateOnly EffectiveFrom { get; private set; }
    public decimal CaloriesKcal { get; private set; }
    public decimal ProteinG { get; private set; }
    public decimal FatG { get; private set; }
    public decimal CarbohydratesG { get; private set; }
    public bool IsActive { get; set; }

    public void Update(decimal calories, decimal protein, decimal fat, decimal carbs)
    {
        CaloriesKcal = calories;
        ProteinG = protein;
        FatG = fat;
        CarbohydratesG = carbs;
        IsActive = true;
    }
}

public sealed class Product
{
    private Product() { }

    public Product(string userId, string name, string? brand, decimal calories, decimal protein, decimal fat, decimal carbs, decimal? servingAmount = null, ServingUnit servingUnit = ServingUnit.Gram, decimal? gramsPerPiece = null)
    {
        Id = Guid.NewGuid();
        OwnerUserId = userId;
        Update(name, brand, calories, protein, fat, carbs);
        SetServing(servingAmount, servingUnit, gramsPerPiece);
        Source = ProductSource.Manual;
        IsVerifiedByUser = true;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string? OwnerUserId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Brand { get; private set; }
    public string? Barcode { get; private set; }
    public decimal? DefaultServingAmount { get; private set; }
    public ServingUnit DefaultServingUnit { get; private set; } = ServingUnit.Gram;
    public decimal? GramsPerPiece { get; private set; }
    public decimal CaloriesPer100 { get; private set; }
    public decimal ProteinPer100 { get; private set; }
    public decimal FatPer100 { get; private set; }
    public decimal CarbohydratesPer100 { get; private set; }
    public ProductSource Source { get; private set; }
    public string? ExternalId { get; private set; }
    public bool IsVerifiedByUser { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public void Update(string name, string? brand, decimal calories, decimal protein, decimal fat, decimal carbs)
    {
        Name = name.Trim();
        Brand = string.IsNullOrWhiteSpace(brand) ? null : brand.Trim();
        CaloriesPer100 = calories;
        ProteinPer100 = protein;
        FatPer100 = fat;
        CarbohydratesPer100 = carbs;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetServing(decimal? amount, ServingUnit unit, decimal? gramsPerPiece)
    {
        DefaultServingAmount = amount;
        DefaultServingUnit = unit;
        GramsPerPiece = gramsPerPiece;
    }

    public void MarkImported(string barcode, string externalId)
    {
        Barcode = barcode;
        ExternalId = externalId;
        Source = ProductSource.OpenFoodFacts;
    }
}

public sealed class Meal
{
    private Meal() { }

    public Meal(string userId, string name, DateTime occurredAtUtc, DateOnly localDate)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Name = name.Trim();
        OccurredAtUtc = occurredAtUtc;
        LocalDate = localDate;
        Source = MealSource.Manual;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = null!;
    public DateTime OccurredAtUtc { get; private set; }
    public DateOnly LocalDate { get; private set; }
    public string Name { get; private set; } = null!;
    public MealSource Source { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public List<MealItem> Items { get; private set; } = [];

    public void Replace(string name, DateTime occurredAtUtc, DateOnly localDate, IEnumerable<MealItem> items)
    {
        Name = name.Trim();
        OccurredAtUtc = occurredAtUtc;
        LocalDate = localDate;
        Items.Clear();
        Items.AddRange(items);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsAssistantDraft() => Source = MealSource.AssistantDraft;
}

public sealed class MealItem
{
    private MealItem() { }

    public MealItem(Guid productId, string name, decimal amount, decimal calories, decimal protein, decimal fat, decimal carbs, bool estimated)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        ProductNameSnapshot = name;
        AmountGrams = amount;
        CaloriesKcal = calories;
        ProteinG = protein;
        FatG = fat;
        CarbohydratesG = carbs;
        IsEstimated = estimated;
    }

    public Guid Id { get; private set; }
    public Guid MealId { get; private set; }
    public Guid? ProductId { get; private set; }
    public string ProductNameSnapshot { get; private set; } = null!;
    public decimal AmountGrams { get; private set; }
    public decimal CaloriesKcal { get; private set; }
    public decimal ProteinG { get; private set; }
    public decimal FatG { get; private set; }
    public decimal CarbohydratesG { get; private set; }
    public bool IsEstimated { get; private set; }
}

public sealed class NutritionDayReview
{
    private NutritionDayReview() { }

    public NutritionDayReview(string userId, DateOnly localDate, NutritionDayStatus status)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        LocalDate = localDate;
        Status = status;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = null!;
    public DateOnly LocalDate { get; private set; }
    public NutritionDayStatus Status { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public void Change(NutritionDayStatus status)
    {
        Status = status;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
