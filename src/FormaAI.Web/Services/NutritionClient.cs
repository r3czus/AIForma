using System.Net.Http.Json;
using FormaAI.Contracts.Nutrition;
using FormaAI.Contracts.Users;

namespace FormaAI.Web.Services;

public sealed class NutritionClient(HttpClient http)
{
    public async Task<NutritionDayResponse> GetDay(DateOnly date) =>
        (await http.GetFromJsonAsync<NutritionDayResponse>($"api/v1/nutrition/days/{date:yyyy-MM-dd}"))!;

    public async Task<IReadOnlyList<ProductResponse>> GetProducts(string? query = null) =>
        await http.GetFromJsonAsync<List<ProductResponse>>($"api/v1/products?query={Uri.EscapeDataString(query ?? string.Empty)}") ?? [];

    public async Task<BarcodeLookupResponse> GetByBarcode(string barcode)
    {
        using var response = await http.GetAsync($"api/v1/products/barcode/{Uri.EscapeDataString(barcode)}");
        if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            return (await response.Content.ReadFromJsonAsync<BarcodeLookupResponse>())!;
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BarcodeLookupResponse>())!;
    }

    public Task<NutritionTargetResponse> SaveTarget(SaveNutritionTargetRequest request) =>
        Send<NutritionTargetResponse>(HttpMethod.Post, "api/v1/nutrition-targets", request);

    public Task<ProductResponse> SaveProduct(SaveProductRequest request) =>
        Send<ProductResponse>(HttpMethod.Post, "api/v1/products", request);

    public Task<MealResponse> SaveMeal(SaveMealRequest request) =>
        Send<MealResponse>(HttpMethod.Post, "api/v1/meals", request);

    public async Task DeleteMeal(Guid id)
    {
        using var response = await Send(HttpMethod.Delete, $"api/v1/meals/{id}", null);
        response.EnsureSuccessStatusCode();
    }

    private async Task<T> Send<T>(HttpMethod method, string uri, object body)
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
