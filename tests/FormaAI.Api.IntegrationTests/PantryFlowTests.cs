using System.Net;
using System.Net.Http.Json;
using FormaAI.Contracts.Nutrition;
using FormaAI.Contracts.Pantry;
using FormaAI.Contracts.Users;
using FormaAI.Domain.Nutrition;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FormaAI.Api.IntegrationTests;

public sealed class PantryFlowTests : IClassFixture<FormaAiFactory>
{
    private readonly FormaAiFactory _factory;
    public PantryFlowTests(FormaAiFactory factory) => _factory = factory;

    [Fact]
    public async Task RecipeMissingItemsShoppingAndMealDeductionUsePantryUnits()
    {
        var options = new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") };
        var owner = _factory.CreateClient(options);
        var other = _factory.CreateClient(options);
        await Register(owner, "pantry-owner@example.test");
        await Register(other, "pantry-other@example.test");
        var product = await Send<SaveProductRequest, ProductResponse>(owner, HttpMethod.Post, "api/v1/products", new("Bułka", null, 250, 8, 3, 45, 1, ServingUnit.Piece, 50));

        await Send<SavePantryItemRequest, PantryItemResponse>(owner, HttpMethod.Put, "api/v1/pantry/items", new(product.Id, 4, ServingUnit.Piece, null));
        var recipe = await Send<SaveRecipeRequest, RecipeResponse>(owner, HttpMethod.Post, "api/v1/recipes", new("Kanapki", null, 2, 10, [new(product.Id, 250, ServingUnit.Gram)]));
        var missing = await owner.GetFromJsonAsync<List<MissingIngredientResponse>>($"api/v1/recipes/{recipe.Id}/missing-ingredients");
        Assert.Equal(50m, missing!.Single().MissingQuantity);

        var list = await Send<object?, ShoppingListResponse>(owner, HttpMethod.Post, $"api/v1/recipes/{recipe.Id}/missing-ingredients/to-shopping-list", null);
        var shoppingItem = list.Items.Single();
        list = await Send<SetPurchasedRequest, ShoppingListResponse>(owner, HttpMethod.Put, $"api/v1/shopping-lists/active/items/{shoppingItem.Id}/purchased", new(true, true));
        Assert.True(list.Items.Single().IsPurchased);
        Assert.Equal(5m, (await owner.GetFromJsonAsync<List<PantryItemResponse>>("api/v1/pantry"))!.Single().Quantity);

        await Send<SaveMealRequest, MealResponse>(owner, HttpMethod.Post, "api/v1/meals", new("Posiłek", DateTimeOffset.UtcNow, [new(product.Id, 100, false)], true));
        Assert.Equal(3m, (await owner.GetFromJsonAsync<List<PantryItemResponse>>("api/v1/pantry"))!.Single().Quantity);

        Assert.Empty((await other.GetFromJsonAsync<List<PantryItemResponse>>("api/v1/pantry"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"api/v1/recipes/{recipe.Id}/missing-ingredients")).StatusCode);
    }

    private static async Task Register(HttpClient client, string email) =>
        _ = await Send<RegisterRequest, CurrentUserResponse>(client, HttpMethod.Post, "api/account/register", new(email, "FormaAI!123", "UTC"));

    private static async Task<TResponse> Send<TRequest, TResponse>(HttpClient client, HttpMethod method, string uri, TRequest body)
    {
        using var response = await client.SendAsync(await Request(client, method, uri, body));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TResponse>())!;
    }

    private static async Task<HttpRequestMessage> Request(HttpClient client, HttpMethod method, string uri, object? body)
    {
        var csrf = await client.GetFromJsonAsync<AntiforgeryResponse>("api/account/antiforgery");
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        if (body is not null) request.Content = JsonContent.Create(body);
        return request;
    }
}
