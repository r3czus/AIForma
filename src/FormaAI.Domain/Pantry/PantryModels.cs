using FormaAI.Domain.Nutrition;

namespace FormaAI.Domain.Pantry;

public enum ShoppingListStatus { Active, Completed }
public enum ShoppingCategory { Protein, Carbohydrates, Fats, FruitAndVegetables, Other }

public sealed class PantryItem
{
    private PantryItem() { }
    public PantryItem(string userId, Guid productId, decimal quantity, ServingUnit unit, DateOnly? expiresOn)
    {
        Id = Guid.NewGuid(); UserId = userId; ProductId = productId;
        Set(quantity, unit, expiresOn);
    }
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public Guid ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public ServingUnit Unit { get; private set; }
    public DateOnly? ExpiresOn { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public void Set(decimal quantity, ServingUnit unit, DateOnly? expiresOn) { Quantity = quantity; Unit = unit; ExpiresOn = expiresOn; }
    public void ChangeBy(decimal amount) => Quantity += amount;
}

public sealed class Recipe
{
    private Recipe() { }
    public Recipe(string userId, string name, string? description, int servings, int preparationMinutes)
    {
        Id = Guid.NewGuid(); UserId = userId; Name = name.Trim(); Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Servings = servings; PreparationMinutes = preparationMinutes; CreatedAtUtc = DateTime.UtcNow;
    }
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int Servings { get; private set; }
    public int PreparationMinutes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public List<RecipeIngredient> Ingredients { get; private set; } = [];
}

public sealed class RecipeIngredient
{
    private RecipeIngredient() { }
    public RecipeIngredient(Guid productId, decimal quantity, ServingUnit unit, int order)
    {
        Id = Guid.NewGuid(); ProductId = productId; Quantity = quantity; Unit = unit; Order = order;
    }
    public Guid Id { get; private set; }
    public Guid RecipeId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public ServingUnit Unit { get; private set; }
    public int Order { get; private set; }
}

public sealed class ShoppingList
{
    private ShoppingList() { }
    public ShoppingList(string userId, string name) { Id = Guid.NewGuid(); UserId = userId; Name = name.Trim(); CreatedAtUtc = DateTime.UtcNow; }
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public ShoppingListStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public List<ShoppingListItem> Items { get; private set; } = [];
}

public sealed class ShoppingListItem
{
    private ShoppingListItem() { }
    public ShoppingListItem(Guid? productId, string name, decimal quantity, ServingUnit unit, ShoppingCategory category)
    {
        Id = Guid.NewGuid(); ProductId = productId; Name = name.Trim(); Quantity = quantity; Unit = unit; Category = category;
    }
    public Guid Id { get; private set; }
    public Guid ShoppingListId { get; private set; }
    public Guid? ProductId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public ServingUnit Unit { get; private set; }
    public ShoppingCategory Category { get; private set; }
    public bool IsPurchased { get; private set; }
    public void SetPurchased(bool purchased) => IsPurchased = purchased;
}
