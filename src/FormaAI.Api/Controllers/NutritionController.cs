using System.Security.Claims;
using FormaAI.Application.Nutrition;
using FormaAI.Application.Assistant;
using FormaAI.Contracts.Nutrition;
using FormaAI.Domain.Nutrition;
using FormaAI.Infrastructure.Persistence;
using FormaAI.Infrastructure.External;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormaAI.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class NutritionController(AppDbContext db, OpenFoodFactsClient openFoodFacts, IAssistantModel assistant) : ControllerBase
{
    [HttpGet("nutrition-targets/current")]
    public async Task<ActionResult<NutritionTargetResponse>> GetCurrentTarget()
    {
        var userId = UserId();
        var date = await LocalToday(userId);
        var target = await db.NutritionTargets
            .Where(x => x.UserId == userId && x.EffectiveFrom <= date)
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync();
        return target is null ? NotFound() : TargetResponse(target);
    }

    [HttpPost("nutrition-targets")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<NutritionTargetResponse>> SaveTarget(SaveNutritionTargetRequest request)
    {
        var userId = UserId();
        var active = await db.NutritionTargets.Where(x => x.UserId == userId && x.IsActive).ToListAsync();
        foreach (var previous in active) previous.IsActive = false;
        var target = new NutritionTarget(userId, request.EffectiveFrom, request.CaloriesKcal, request.ProteinG, request.FatG, request.CarbohydratesG);
        db.NutritionTargets.Add(target);
        await db.SaveChangesAsync();
        return Created("api/v1/nutrition-targets/current", TargetResponse(target));
    }

    [HttpGet("products")]
    public async Task<IReadOnlyList<ProductResponse>> Products([FromQuery] string? query)
    {
        var userId = UserId();
        var products = db.Products.Where(x => x.OwnerUserId == null || x.OwnerUserId == userId);
        if (!string.IsNullOrWhiteSpace(query))
        {
            products = products.Where(x => x.Name.Contains(query));
        }

        return await products.OrderBy(x => x.Name).Take(30)
            .Select(x => new ProductResponse(x.Id, x.Name, x.Brand, x.CaloriesPer100, x.ProteinPer100, x.FatPer100, x.CarbohydratesPer100, x.DefaultServingAmount, x.DefaultServingUnit, x.GramsPerPiece, x.Barcode))
            .ToListAsync();
    }

    [HttpPost("products")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<ProductResponse>> CreateProduct(SaveProductRequest request)
    {
        var product = new Product(UserId(), request.Name, request.Brand, request.CaloriesPer100, request.ProteinPer100, request.FatPer100, request.CarbohydratesPer100, request.DefaultServingAmount, request.DefaultServingUnit, request.GramsPerPiece);
        if (request.Barcode is not null) product.MarkImported(request.Barcode, request.Barcode);
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return Created($"api/v1/products/{product.Id}", ProductResponse(product));
    }

    [HttpGet("products/barcode/{barcode}")]
    public async Task<ActionResult<BarcodeLookupResponse>> ProductByBarcode(string barcode, CancellationToken cancellationToken)
    {
        if (barcode.Length is < 8 or > 14 || barcode.Any(x => !char.IsDigit(x))) return BadRequest("Kod kreskowy musi zawierać od 8 do 14 cyfr.");
        var userId = UserId();
        var local = await db.Products.FirstOrDefaultAsync(x => x.Barcode == barcode && (x.OwnerUserId == null || x.OwnerUserId == userId), cancellationToken);
        if (local is not null) return new BarcodeLookupResponse(true, true, false, "Produkt znaleziony w lokalnej bazie.", Draft(local));
        var result = await openFoodFacts.FindProduct(barcode, cancellationToken);
        if (result.Status == OpenFoodFactsResultStatus.Unavailable) return StatusCode(StatusCodes.Status503ServiceUnavailable, new BarcodeLookupResponse(false, false, true, "Open Food Facts jest chwilowo niedostępne. Spróbuj ponownie później.", null));
        if (result.Product is null) return new BarcodeLookupResponse(false, false, false, "Nie znaleziono produktu. Możesz dodać go ręcznie.", null);
        var source = result.Product;
        var unit = source.ServingUnit?.Equals("ml", StringComparison.OrdinalIgnoreCase) == true ? ServingUnit.Milliliter : ServingUnit.Gram;
        return new BarcodeLookupResponse(true, false, false, "Sprawdź dane z Open Food Facts przed zapisem.", new ProductImportDraft(source.Barcode, source.Name, source.Brand, source.CaloriesPer100, source.ProteinPer100, source.FatPer100, source.CarbohydratesPer100, source.ServingQuantity, unit));
    }

    [HttpPut("products/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<ProductResponse>> UpdateProduct(Guid id, SaveProductRequest request)
    {
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == id && x.OwnerUserId == UserId());
        if (product is null) return NotFound();
        product.Update(request.Name, request.Brand, request.CaloriesPer100, request.ProteinPer100, request.FatPer100, request.CarbohydratesPer100);
        product.SetServing(request.DefaultServingAmount, request.DefaultServingUnit, request.GramsPerPiece);
        await db.SaveChangesAsync();
        return ProductResponse(product);
    }

    [HttpGet("nutrition/days/{date}")]
    public async Task<ActionResult<NutritionDayResponse>> Day(DateOnly date)
    {
        var userId = UserId();
        var target = await db.NutritionTargets.Where(x => x.UserId == userId && x.EffectiveFrom <= date)
            .OrderByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync();
        var meals = await db.Meals.Include(x => x.Items)
            .Where(x => x.UserId == userId && x.LocalDate == date)
            .OrderBy(x => x.OccurredAtUtc).ToListAsync();
        var mealResponses = meals.Select(MealResponse).ToList();
        var consumed = mealResponses.Aggregate(new Macro(), (sum, meal) => sum + ToMacro(meal.Macro));
        MacroResponse? targetMacro = target is null ? null : MacroResponse(target);
        MacroResponse? remaining = target is null ? null : MacroResponse(ToMacro(targetMacro!) - consumed);
        return new NutritionDayResponse(date, targetMacro, MacroResponse(consumed), remaining, mealResponses);
    }

    [HttpGet("meals/recent")]
    public async Task<IReadOnlyList<MealResponse>> RecentMeals([FromQuery] int take = 8)
    {
        var meals = await db.Meals.Include(x => x.Items).Where(x => x.UserId == UserId())
            .OrderByDescending(x => x.OccurredAtUtc).Take(60).ToListAsync();
        return meals.GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Select(x => x.First()).Take(Math.Clamp(take, 1, 20)).Select(MealResponse).ToList();
    }

    [HttpPost("meals")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<MealResponse>> CreateMeal(SaveMealRequest request)
    {
        var userId = UserId();
        var meal = await BuildMeal(userId, request);
        if (meal is null) return ValidationProblem("Co najmniej jeden produkt nie istnieje lub nie należy do użytkownika.");
        if (request.DeductFromPantry && !await DeductFromPantry(userId, meal)) return Conflict("W spiżarni nie ma wystarczającej ilości składników.");
        db.Meals.Add(meal);
        await db.SaveChangesAsync();
        return Created($"api/v1/meals/{meal.Id}", MealResponse(meal));
    }

    [HttpPost("nutrition/meal-photo")]
    [ValidateAntiForgeryToken]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<ActionResult<MealPhotoDraftResponse>> AnalyzePhoto([FromForm] IFormFile photo, CancellationToken cancellationToken)
    {
        var mime = photo.ContentType.ToLowerInvariant();
        if (photo.Length is 0 or > 12 * 1024 * 1024) return BadRequest("Zdjęcie może mieć maksymalnie 12 MB.");
        if (mime is not ("image/jpeg" or "image/png" or "image/webp" or "image/heic" or "image/heif"))
            return BadRequest("Obsługiwane formaty to JPEG, PNG, WEBP, HEIC i HEIF.");
        await using var stream = new MemoryStream((int)photo.Length);
        await photo.CopyToAsync(stream, cancellationToken);
        try { return await assistant.AnalyzeMealPhoto(stream.ToArray(), mime, cancellationToken); }
        catch (AssistantModelUnavailableException ex) { return StatusCode(StatusCodes.Status503ServiceUnavailable, ex.Message); }
    }

    [HttpPost("nutrition/meal-text")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<MealPhotoDraftResponse>> AnalyzeText(AnalyzeMealTextRequest request, CancellationToken cancellationToken)
    {
        try { return await assistant.AnalyzeMealText(request.Description, cancellationToken); }
        catch (AssistantModelUnavailableException ex) { return StatusCode(StatusCodes.Status503ServiceUnavailable, ex.Message); }
    }

    [HttpPost("meals/estimated")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<MealResponse>> CreateEstimatedMeal(SaveEstimatedMealRequest request)
    {
        var userId = UserId();
        var names = request.Items.Select(x => x.Name.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var known = await db.Products.Where(x => (x.OwnerUserId == null || x.OwnerUserId == userId) && names.Contains(x.Name)).ToListAsync();
        var products = known.GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        var localDate = await LocalDate(userId, request.OccurredAt.UtcDateTime);
        var meal = new Meal(userId, request.Name, request.OccurredAt.UtcDateTime, localDate);
        meal.MarkAsAssistantDraft();

        foreach (var item in request.Items)
        {
            if (!products.TryGetValue(item.Name.Trim(), out var product))
            {
                product = new Product(userId, item.Name, null, item.CaloriesPer100, item.ProteinPer100, item.FatPer100, item.CarbohydratesPer100, item.AmountGrams);
                db.Products.Add(product);
                products[product.Name] = product;
            }
            var macro = NutritionCalculator.ForProduct(product, item.AmountGrams);
            meal.Items.Add(new MealItem(product.Id, product.Name, item.AmountGrams, macro.CaloriesKcal, macro.ProteinG, macro.FatG, macro.CarbohydratesG, true));
        }

        db.Meals.Add(meal);
        await db.SaveChangesAsync();
        return Created($"api/v1/meals/{meal.Id}", MealResponse(meal));
    }

    [HttpPut("meals/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<MealResponse>> UpdateMeal(Guid id, SaveMealRequest request)
    {
        var userId = UserId();
        var existing = await db.Meals.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (existing is null) return NotFound();
        var changed = await BuildMeal(userId, request);
        if (changed is null) return ValidationProblem("Co najmniej jeden produkt nie istnieje lub nie należy do użytkownika.");
        existing.Replace(changed.Name, changed.OccurredAtUtc, changed.LocalDate, changed.Items);
        await db.SaveChangesAsync();
        return MealResponse(existing);
    }

    [HttpDelete("meals/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMeal(Guid id)
    {
        var meal = await db.Meals.SingleOrDefaultAsync(x => x.Id == id && x.UserId == UserId());
        if (meal is null) return NotFound();
        db.Meals.Remove(meal);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<Meal?> BuildMeal(string userId, SaveMealRequest request)
    {
        var ids = request.Items.Select(x => x.ProductId).Distinct().ToList();
        var products = await db.Products.Where(x => ids.Contains(x.Id) && (x.OwnerUserId == null || x.OwnerUserId == userId)).ToDictionaryAsync(x => x.Id);
        if (products.Count != ids.Count) return null;
        var localDate = await LocalDate(userId, request.OccurredAt.UtcDateTime);
        var meal = new Meal(userId, request.Name, request.OccurredAt.UtcDateTime, localDate);
        foreach (var item in request.Items)
        {
            var product = products[item.ProductId];
            var macro = NutritionCalculator.ForProduct(product, item.AmountGrams);
            meal.Items.Add(new MealItem(product.Id, product.Name, item.AmountGrams, macro.CaloriesKcal, macro.ProteinG, macro.FatG, macro.CarbohydratesG, item.IsEstimated));
        }
        return meal;
    }

    private async Task<bool> DeductFromPantry(string userId, Meal meal)
    {
        var productIds = meal.Items.Select(x => x.ProductId!.Value).Distinct().ToList();
        var pantry = await db.PantryItems.Where(x => x.UserId == userId && productIds.Contains(x.ProductId)).ToDictionaryAsync(x => x.ProductId);
        var products = await db.Products.Where(x => productIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        foreach (var mealItem in meal.Items)
        {
            var productId = mealItem.ProductId!.Value;
            if (!pantry.TryGetValue(productId, out var pantryItem)) return false;
            var amount = UnitConverter.Convert(mealItem.AmountGrams, ServingUnit.Gram, pantryItem.Unit, products[productId].GramsPerPiece);
            if (amount is null || pantryItem.Quantity < amount.Value) return false;
            pantryItem.ChangeBy(-amount.Value);
        }
        return true;
    }

    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private async Task<DateOnly> LocalToday(string userId) => await LocalDate(userId, DateTime.UtcNow);
    private async Task<DateOnly> LocalDate(string userId, DateTime utc)
    {
        var zoneId = await db.UserProfiles.Where(x => x.UserId == userId).Select(x => x.TimeZoneId).SingleAsync();
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.FindSystemTimeZoneById(zoneId)));
    }

    private static NutritionTargetResponse TargetResponse(NutritionTarget x) => new(x.Id, x.EffectiveFrom, x.CaloriesKcal, x.ProteinG, x.FatG, x.CarbohydratesG);
    private static ProductResponse ProductResponse(Product x) => new(x.Id, x.Name, x.Brand, x.CaloriesPer100, x.ProteinPer100, x.FatPer100, x.CarbohydratesPer100, x.DefaultServingAmount, x.DefaultServingUnit, x.GramsPerPiece, x.Barcode);
    private static ProductImportDraft Draft(Product x) => new(x.Barcode!, x.Name, x.Brand, x.CaloriesPer100, x.ProteinPer100, x.FatPer100, x.CarbohydratesPer100, x.DefaultServingAmount, x.DefaultServingUnit);
    private static MacroResponse MacroResponse(NutritionTarget x) => new(x.CaloriesKcal, x.ProteinG, x.FatG, x.CarbohydratesG);
    private static MacroResponse MacroResponse(Macro x) => new(x.CaloriesKcal, x.ProteinG, x.FatG, x.CarbohydratesG);
    private static Macro ToMacro(MacroResponse x) => new(x.CaloriesKcal, x.ProteinG, x.FatG, x.CarbohydratesG);
    private static MealResponse MealResponse(Meal meal)
    {
        var items = meal.Items.Select(x => new MealItemResponse(x.Id, x.ProductId, x.ProductNameSnapshot, x.AmountGrams, new MacroResponse(x.CaloriesKcal, x.ProteinG, x.FatG, x.CarbohydratesG), x.IsEstimated)).ToList();
        var macro = items.Aggregate(new Macro(), (sum, item) => sum + ToMacro(item.Macro));
        return new MealResponse(meal.Id, meal.Name, meal.OccurredAtUtc, items, MacroResponse(macro));
    }
}
