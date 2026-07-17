using System.Net.Http.Json;
using FormaAI.Contracts.Nutrition;
using FormaAI.Contracts.Users;
using FormaAI.Domain.Nutrition;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FormaAI.Api.IntegrationTests;

public sealed class NutritionFlowTests : IClassFixture<FormaAiFactory>
{
    private readonly FormaAiFactory _factory;

    public NutritionFlowTests(FormaAiFactory factory) => _factory = factory;

    [Fact]
    public async Task MealUsesProductSnapshotAndStaysPrivate()
    {
        var options = new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") };
        var owner = _factory.CreateClient(options);
        var other = _factory.CreateClient(options);
        await Register(owner, "nutrition-owner@example.test");
        await Register(other, "nutrition-other@example.test");
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        await Send<SaveNutritionTargetRequest, NutritionTargetResponse>(owner, HttpMethod.Post, "api/v1/nutrition-targets", new(date, 2000, 150, 70, 220));
        var product = await Send<SaveProductRequest, ProductResponse>(owner, HttpMethod.Post, "api/v1/products", new("Skyr", null, 64.4m, 12.3m, 0.2m, 3.8m, null, ServingUnit.Gram, null, "12345678"));
        var barcode = await owner.GetFromJsonAsync<BarcodeLookupResponse>("api/v1/products/barcode/12345678");
        Assert.True(barcode!.FromLocalDatabase);
        await Send<SaveMealRequest, MealResponse>(owner, HttpMethod.Post, "api/v1/meals", new("Śniadanie", DateTimeOffset.UtcNow, [new(product.Id, 180, false)]));

        var beforeEdit = await owner.GetFromJsonAsync<NutritionDayResponse>($"api/v1/nutrition/days/{date:yyyy-MM-dd}");
        Assert.Equal(115.92m, beforeEdit!.Consumed.CaloriesKcal);
        Assert.Equal(22.14m, beforeEdit.Consumed.ProteinG);
        Assert.Equal(1884.08m, beforeEdit.Remaining!.CaloriesKcal);

        await Send<SaveProductRequest, ProductResponse>(owner, HttpMethod.Put, $"api/v1/products/{product.Id}", new("Skyr po edycji", null, 100, 20, 1, 5));
        var afterEdit = await owner.GetFromJsonAsync<NutritionDayResponse>($"api/v1/nutrition/days/{date:yyyy-MM-dd}");
        Assert.Equal(115.92m, afterEdit!.Consumed.CaloriesKcal);
        Assert.Equal("Skyr", afterEdit.Meals.Single().Items.Single().ProductName);

        var otherProducts = await other.GetFromJsonAsync<List<ProductResponse>>("api/v1/products");
        var otherDay = await other.GetFromJsonAsync<NutritionDayResponse>($"api/v1/nutrition/days/{date:yyyy-MM-dd}");
        Assert.Empty(otherProducts!);
        Assert.Empty(otherDay!.Meals);
    }

    private static async Task Register(HttpClient client, string email) =>
        _ = await Send<RegisterRequest, CurrentUserResponse>(client, HttpMethod.Post, "api/account/register", new(email, "FormaAI!123", "UTC"));

    private static async Task<TResponse> Send<TRequest, TResponse>(HttpClient client, HttpMethod method, string uri, TRequest body)
    {
        var csrf = await client.GetFromJsonAsync<AntiforgeryResponse>("api/account/antiforgery");
        var request = new HttpRequestMessage(method, uri) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TResponse>())!;
    }
}
