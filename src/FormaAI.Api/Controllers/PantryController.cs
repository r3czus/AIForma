using System.Security.Claims;
using FormaAI.Application.Nutrition;
using FormaAI.Contracts.Nutrition;
using FormaAI.Contracts.Pantry;
using FormaAI.Domain.Nutrition;
using FormaAI.Domain.Pantry;
using FormaAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormaAI.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class PantryController(AppDbContext db) : ControllerBase
{
    [HttpGet("pantry")]
    public async Task<IReadOnlyList<PantryItemResponse>> Pantry()
    {
        var userId = UserId();
        return await (from item in db.PantryItems
                      join product in db.Products on item.ProductId equals product.Id
                      where item.UserId == userId
                      orderby product.Name
                      select new PantryItemResponse(item.Id, product.Id, product.Name, item.Quantity, item.Unit, item.ExpiresOn)).ToListAsync();
    }

    [HttpPut("pantry/items")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<PantryItemResponse>> SavePantryItem(SavePantryItemRequest request)
    {
        var userId = UserId();
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == request.ProductId && (x.OwnerUserId == null || x.OwnerUserId == userId));
        if (product is null) return NotFound();
        var item = await db.PantryItems.SingleOrDefaultAsync(x => x.UserId == userId && x.ProductId == request.ProductId);
        if (item is null)
        {
            item = new PantryItem(userId, request.ProductId, request.Quantity, request.Unit, request.ExpiresOn);
            db.PantryItems.Add(item);
        }
        else item.Set(request.Quantity, request.Unit, request.ExpiresOn);
        await db.SaveChangesAsync();
        return new PantryItemResponse(item.Id, product.Id, product.Name, item.Quantity, item.Unit, item.ExpiresOn);
    }

    [HttpDelete("pantry/items/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePantryItem(Guid id)
    {
        var item = await db.PantryItems.SingleOrDefaultAsync(x => x.Id == id && x.UserId == UserId());
        if (item is null) return NotFound();
        db.PantryItems.Remove(item);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("recipes")]
    public async Task<IReadOnlyList<RecipeResponse>> Recipes()
    {
        var recipes = await db.Recipes.Include(x => x.Ingredients).Where(x => x.UserId == UserId()).OrderBy(x => x.Name).ToListAsync();
        return await RecipeResponses(recipes);
    }

    [HttpPost("recipes")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<RecipeResponse>> CreateRecipe(SaveRecipeRequest request)
    {
        var userId = UserId();
        var productIds = request.Ingredients.Select(x => x.ProductId).Distinct().ToList();
        var count = await db.Products.CountAsync(x => productIds.Contains(x.Id) && (x.OwnerUserId == null || x.OwnerUserId == userId));
        if (count != productIds.Count) return ValidationProblem("Przepis zawiera niedostępny produkt.");
        var recipe = new Recipe(userId, request.Name, request.Description, request.Servings, request.PreparationMinutes);
        for (var i = 0; i < request.Ingredients.Count; i++)
        {
            var ingredient = request.Ingredients[i];
            recipe.Ingredients.Add(new RecipeIngredient(ingredient.ProductId, ingredient.Quantity, ingredient.Unit, i + 1));
        }
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();
        return Created($"api/v1/recipes/{recipe.Id}", (await RecipeResponses([recipe])).Single());
    }

    [HttpGet("recipes/{id:guid}/missing-ingredients")]
    public async Task<ActionResult<IReadOnlyList<MissingIngredientResponse>>> MissingIngredients(Guid id)
    {
        var recipe = await db.Recipes.Include(x => x.Ingredients).SingleOrDefaultAsync(x => x.Id == id && x.UserId == UserId());
        if (recipe is null) return NotFound();
        return Ok(await Missing(recipe));
    }

    [HttpPost("recipes/{id:guid}/missing-ingredients/to-shopping-list")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<ShoppingListResponse>> AddMissingToShopping(Guid id)
    {
        var recipe = await db.Recipes.Include(x => x.Ingredients).SingleOrDefaultAsync(x => x.Id == id && x.UserId == UserId());
        if (recipe is null) return NotFound();
        var missing = await Missing(recipe);
        var list = await ActiveList();
        foreach (var ingredient in missing)
        {
            var existing = list.Items.FirstOrDefault(x => x.ProductId == ingredient.ProductId && !x.IsPurchased && x.Unit == ingredient.Unit);
            if (existing is null)
            {
                var item = new ShoppingListItem(ingredient.ProductId, ingredient.ProductName, ingredient.MissingQuantity, ingredient.Unit, ShoppingCategory.Other);
                list.Items.Add(item);
                db.ShoppingListItems.Add(item);
            }
        }
        await db.SaveChangesAsync();
        return ListResponse(list);
    }

    [HttpGet("shopping-lists/active")]
    public async Task<ActionResult<ShoppingListResponse>> ShoppingList() => ListResponse(await ActiveList());

    [HttpPost("shopping-lists/active/items")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<ShoppingListResponse>> AddShoppingItem(SaveShoppingItemRequest request)
    {
        var userId = UserId();
        if (request.ProductId is Guid productId && !await db.Products.AnyAsync(x => x.Id == productId && (x.OwnerUserId == null || x.OwnerUserId == userId))) return NotFound();
        var list = await ActiveList();
        var item = new ShoppingListItem(request.ProductId, request.Name, request.Quantity, request.Unit, request.Category);
        list.Items.Add(item);
        db.ShoppingListItems.Add(item);
        await db.SaveChangesAsync();
        return ListResponse(list);
    }

    [HttpPut("shopping-lists/active/items/{itemId:guid}/purchased")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<ShoppingListResponse>> SetPurchased(Guid itemId, SetPurchasedRequest request)
    {
        var list = await ActiveList();
        var item = list.Items.SingleOrDefault(x => x.Id == itemId);
        if (item is null) return NotFound();
        item.SetPurchased(request.IsPurchased);
        if (request.IsPurchased && request.MoveToPantry && item.ProductId is Guid productId)
        {
            var pantry = await db.PantryItems.SingleOrDefaultAsync(x => x.UserId == UserId() && x.ProductId == productId);
            if (pantry is null) db.PantryItems.Add(new PantryItem(UserId(), productId, item.Quantity, item.Unit, null));
            else
            {
                var product = await db.Products.SingleAsync(x => x.Id == productId);
                var converted = UnitConverter.Convert(item.Quantity, item.Unit, pantry.Unit, product.GramsPerPiece);
                if (converted is null) return Conflict("Nie można przeliczyć jednostki kupionego produktu.");
                pantry.ChangeBy(converted.Value);
            }
        }
        await db.SaveChangesAsync();
        return ListResponse(list);
    }

    private async Task<ShoppingList> ActiveList()
    {
        var userId = UserId();
        var list = await db.ShoppingLists.Include(x => x.Items).SingleOrDefaultAsync(x => x.UserId == userId && x.Status == ShoppingListStatus.Active);
        if (list is not null) return list;
        list = new ShoppingList(userId, "Moja lista");
        db.ShoppingLists.Add(list);
        await db.SaveChangesAsync();
        return list;
    }

    private async Task<IReadOnlyList<MissingIngredientResponse>> Missing(Recipe recipe)
    {
        var productIds = recipe.Ingredients.Select(x => x.ProductId).Distinct().ToList();
        var products = await db.Products.AsNoTracking().Where(x => productIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        var pantry = await db.PantryItems.AsNoTracking().Where(x => x.UserId == UserId() && productIds.Contains(x.ProductId)).ToDictionaryAsync(x => x.ProductId);
        var missing = new List<MissingIngredientResponse>();
        foreach (var ingredient in recipe.Ingredients.OrderBy(x => x.Order))
        {
            var product = products[ingredient.ProductId];
            var available = pantry.TryGetValue(product.Id, out var item) ? UnitConverter.Convert(item.Quantity, item.Unit, ingredient.Unit, product.GramsPerPiece) ?? 0 : 0;
            if (available < ingredient.Quantity) missing.Add(new(product.Id, product.Name, ingredient.Quantity - available, ingredient.Unit));
        }
        return missing;
    }

    private async Task<IReadOnlyList<RecipeResponse>> RecipeResponses(IReadOnlyList<Recipe> recipes)
    {
        var productIds = recipes.SelectMany(x => x.Ingredients).Select(x => x.ProductId).Distinct().ToList();
        var products = await db.Products.Where(x => productIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        return recipes.Select(recipe =>
        {
            var ingredients = recipe.Ingredients.OrderBy(x => x.Order).Select(x =>
            {
                var product = products[x.ProductId];
                var grams = UnitConverter.ToGrams(x.Quantity, x.Unit, product.GramsPerPiece) ?? 0;
                var macro = NutritionCalculator.ForProduct(product, grams);
                return new RecipeIngredientResponse(product.Id, product.Name, x.Quantity, x.Unit, new MacroResponse(macro.CaloriesKcal, macro.ProteinG, macro.FatG, macro.CarbohydratesG));
            }).ToList();
            var total = ingredients.Aggregate(new Macro(), (sum, x) => sum + new Macro(x.Macro.CaloriesKcal, x.Macro.ProteinG, x.Macro.FatG, x.Macro.CarbohydratesG));
            return new RecipeResponse(recipe.Id, recipe.Name, recipe.Description, recipe.Servings, recipe.PreparationMinutes, ingredients, new MacroResponse(total.CaloriesKcal, total.ProteinG, total.FatG, total.CarbohydratesG));
        }).ToList();
    }

    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private static ShoppingListResponse ListResponse(ShoppingList list) => new(list.Id, list.Name, list.Status,
        list.Items.OrderBy(x => x.IsPurchased).ThenBy(x => x.Category).ThenBy(x => x.Name).Select(x => new ShoppingItemResponse(x.Id, x.ProductId, x.Name, x.Quantity, x.Unit, x.Category, x.IsPurchased)).ToList());
}
