using System.Net.Http.Json;
using FormaAI.Contracts.Pantry;
using FormaAI.Contracts.Users;

namespace FormaAI.Web.Services;

public sealed class PantryClient(HttpClient http)
{
    public async Task<IReadOnlyList<PantryItemResponse>> GetPantry() => await http.GetFromJsonAsync<List<PantryItemResponse>>("api/v1/pantry") ?? [];
    public async Task<IReadOnlyList<RecipeResponse>> GetRecipes() => await http.GetFromJsonAsync<List<RecipeResponse>>("api/v1/recipes") ?? [];
    public async Task<IReadOnlyList<MissingIngredientResponse>> GetMissing(Guid recipeId) => await http.GetFromJsonAsync<List<MissingIngredientResponse>>($"api/v1/recipes/{recipeId}/missing-ingredients") ?? [];
    public async Task<ShoppingListResponse> GetShoppingList() => (await http.GetFromJsonAsync<ShoppingListResponse>("api/v1/shopping-lists/active"))!;
    public Task<PantryItemResponse> SavePantry(SavePantryItemRequest body) => Send<PantryItemResponse>(HttpMethod.Put, "api/v1/pantry/items", body);
    public Task<RecipeResponse> SaveRecipe(SaveRecipeRequest body) => Send<RecipeResponse>(HttpMethod.Post, "api/v1/recipes", body);
    public Task<ShoppingListResponse> AddShoppingItem(SaveShoppingItemRequest body) => Send<ShoppingListResponse>(HttpMethod.Post, "api/v1/shopping-lists/active/items", body);
    public Task<ShoppingListResponse> AddMissing(Guid recipeId) => Send<ShoppingListResponse>(HttpMethod.Post, $"api/v1/recipes/{recipeId}/missing-ingredients/to-shopping-list", null);
    public Task<ShoppingListResponse> SetPurchased(Guid itemId, SetPurchasedRequest body) => Send<ShoppingListResponse>(HttpMethod.Put, $"api/v1/shopping-lists/active/items/{itemId}/purchased", body);

    public async Task DeletePantry(Guid id)
    {
        using var response = await Send(HttpMethod.Delete, $"api/v1/pantry/items/{id}", null);
        response.EnsureSuccessStatusCode();
    }

    private async Task<T> Send<T>(HttpMethod method, string uri, object? body)
    {
        using var response = await Send(method, uri, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task<HttpResponseMessage> Send(HttpMethod method, string uri, object? body)
    {
        var csrf = await http.GetFromJsonAsync<AntiforgeryResponse>("api/account/antiforgery");
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await http.SendAsync(request);
    }
}
