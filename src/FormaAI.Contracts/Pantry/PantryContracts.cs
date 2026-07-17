using System.ComponentModel.DataAnnotations;
using FormaAI.Contracts.Nutrition;
using FormaAI.Domain.Nutrition;
using FormaAI.Domain.Pantry;

namespace FormaAI.Contracts.Pantry;

public sealed record SavePantryItemRequest(Guid ProductId, [Range(0, 1000000)] decimal Quantity, ServingUnit Unit, DateOnly? ExpiresOn);
public sealed record PantryItemResponse(Guid Id, Guid ProductId, string ProductName, decimal Quantity, ServingUnit Unit, DateOnly? ExpiresOn);
public sealed record RecipeIngredientRequest(Guid ProductId, [Range(0.01, 100000)] decimal Quantity, ServingUnit Unit);
public sealed record SaveRecipeRequest([Required, MaxLength(150)] string Name, [MaxLength(1000)] string? Description, [Range(1, 100)] int Servings, [Range(0, 1440)] int PreparationMinutes, [MinLength(1)] IReadOnlyList<RecipeIngredientRequest> Ingredients);
public sealed record RecipeIngredientResponse(Guid ProductId, string ProductName, decimal Quantity, ServingUnit Unit, MacroResponse Macro);
public sealed record RecipeResponse(Guid Id, string Name, string? Description, int Servings, int PreparationMinutes, IReadOnlyList<RecipeIngredientResponse> Ingredients, MacroResponse Macro);
public sealed record MissingIngredientResponse(Guid ProductId, string ProductName, decimal MissingQuantity, ServingUnit Unit);
public sealed record SaveShoppingItemRequest(Guid? ProductId, [Required, MaxLength(200)] string Name, [Range(0.01, 100000)] decimal Quantity, ServingUnit Unit, ShoppingCategory Category);
public sealed record ShoppingItemResponse(Guid Id, Guid? ProductId, string Name, decimal Quantity, ServingUnit Unit, ShoppingCategory Category, bool IsPurchased);
public sealed record ShoppingListResponse(Guid Id, string Name, ShoppingListStatus Status, IReadOnlyList<ShoppingItemResponse> Items);
public sealed record SetPurchasedRequest(bool IsPurchased, bool MoveToPantry);
