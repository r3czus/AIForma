using System.ComponentModel.DataAnnotations;
using FormaAI.Domain.Nutrition;

namespace FormaAI.Contracts.Nutrition;

public sealed record MacroResponse(decimal CaloriesKcal, decimal ProteinG, decimal FatG, decimal CarbohydratesG);
public sealed record NutritionTargetResponse(Guid Id, DateOnly EffectiveFrom, decimal CaloriesKcal, decimal ProteinG, decimal FatG, decimal CarbohydratesG);
public sealed record SaveNutritionTargetRequest(DateOnly EffectiveFrom, [Range(1, 20000)] decimal CaloriesKcal, [Range(0, 1000)] decimal ProteinG, [Range(0, 1000)] decimal FatG, [Range(0, 2000)] decimal CarbohydratesG);
public sealed record ProductResponse(Guid Id, string Name, string? Brand, decimal CaloriesPer100, decimal ProteinPer100, decimal FatPer100, decimal CarbohydratesPer100, decimal? DefaultServingAmount = null, ServingUnit DefaultServingUnit = ServingUnit.Gram, decimal? GramsPerPiece = null, string? Barcode = null);
public sealed record SaveProductRequest([Required, MaxLength(200)] string Name, [MaxLength(150)] string? Brand, [Range(0, 5000)] decimal CaloriesPer100, [Range(0, 1000)] decimal ProteinPer100, [Range(0, 1000)] decimal FatPer100, [Range(0, 1000)] decimal CarbohydratesPer100, [Range(0.01, 100000)] decimal? DefaultServingAmount = null, ServingUnit DefaultServingUnit = ServingUnit.Gram, [Range(0.01, 100000)] decimal? GramsPerPiece = null, [RegularExpression("^[0-9]{8,14}$")] string? Barcode = null);
public sealed record ProductImportDraft(string Barcode, string Name, string? Brand, decimal CaloriesPer100, decimal ProteinPer100, decimal FatPer100, decimal CarbohydratesPer100, decimal? ServingAmount, ServingUnit ServingUnit);
public sealed record BarcodeLookupResponse(bool Found, bool FromLocalDatabase, bool TemporarilyUnavailable, string Message, ProductImportDraft? Product);
public sealed record MealItemRequest(Guid ProductId, [Range(0.01, 100000)] decimal AmountGrams, bool IsEstimated);
public sealed record SaveMealRequest([Required, MaxLength(120)] string Name, DateTimeOffset OccurredAt, [MinLength(1)] IReadOnlyList<MealItemRequest> Items, bool DeductFromPantry = false);
public sealed record MealItemResponse(Guid Id, Guid? ProductId, string ProductName, decimal AmountGrams, MacroResponse Macro, bool IsEstimated);
public sealed record MealResponse(Guid Id, string Name, DateTime OccurredAtUtc, IReadOnlyList<MealItemResponse> Items, MacroResponse Macro);
public sealed record NutritionDayResponse(DateOnly Date, MacroResponse? Target, MacroResponse Consumed, MacroResponse? Remaining, IReadOnlyList<MealResponse> Meals);
public sealed record MealPhotoItemDraft(string Name, decimal AmountGrams, decimal CaloriesPer100, decimal ProteinPer100, decimal FatPer100, decimal CarbohydratesPer100);
public sealed record MealPhotoDraftResponse(string MealName, string? Note, IReadOnlyList<MealPhotoItemDraft> Items);
public sealed record AnalyzeMealTextRequest([Required, MaxLength(1000)] string Description);
public sealed record SaveEstimatedMealRequest([Required, MaxLength(120)] string Name, DateTimeOffset OccurredAt, [MinLength(1), MaxLength(12)] IReadOnlyList<SaveEstimatedMealItemRequest> Items);
public sealed record SaveEstimatedMealItemRequest([Required, MaxLength(200)] string Name, [Range(0.01, 5000)] decimal AmountGrams, [Range(0, 5000)] decimal CaloriesPer100, [Range(0, 1000)] decimal ProteinPer100, [Range(0, 1000)] decimal FatPer100, [Range(0, 1000)] decimal CarbohydratesPer100);
