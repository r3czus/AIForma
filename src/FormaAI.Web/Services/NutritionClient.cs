using System.Net.Http.Json;
using System.Net.Http.Headers;
using FormaAI.Contracts.Nutrition;
using FormaAI.Contracts.Users;
using FormaAI.Domain.Nutrition;
using Microsoft.AspNetCore.Components.Forms;

namespace FormaAI.Web.Services;

public sealed class NutritionClient(HttpClient http)
{
    public async Task<NutritionTargetResponse?> GetCurrentTarget()
    {
        using var response = await http.GetAsync("api/v1/nutrition-targets/current");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<NutritionTargetResponse>();
    }

    public async Task<NutritionDayResponse> GetDay(DateOnly date) =>
        (await http.GetFromJsonAsync<NutritionDayResponse>($"api/v1/nutrition/days/{date:yyyy-MM-dd}"))!;

    public async Task SaveDayStatus(DateOnly date, NutritionDayStatus status)
    {
        using var response = await Send(HttpMethod.Put, $"api/v1/nutrition/days/{date:yyyy-MM-dd}/status", new SaveNutritionDayStatusRequest(status));
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<MealResponse>> GetRecentMeals() =>
        await http.GetFromJsonAsync<List<MealResponse>>("api/v1/meals/recent") ?? [];

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

    public Task<MealResponse> UpdateMeal(Guid id, SaveMealRequest request) =>
        Send<MealResponse>(HttpMethod.Put, $"api/v1/meals/{id}", request);

    public async Task<MealPhotoDraftResponse> AnalyzeMealText(string description)
    {
        using var response = await Send(HttpMethod.Post, "api/v1/nutrition/meal-text", new AnalyzeMealTextRequest(description));
        return await ReadAnalysis(response);
    }

    public async Task<MealPhotoDraftResponse> AnalyzeMealPhoto(IBrowserFile photo)
    {
        var csrf = await http.GetFromJsonAsync<AntiforgeryResponse>("api/account/antiforgery");
        using var content = new MultipartFormDataContent();
        await using var stream = photo.OpenReadStream(12 * 1024 * 1024);
        using var file = new StreamContent(stream);
        file.Headers.ContentType = new MediaTypeHeaderValue(photo.ContentType);
        content.Add(file, "photo", photo.Name);
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/nutrition/meal-photo") { Content = content };
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        using var response = await http.SendAsync(request);
        return await ReadAnalysis(response);
    }

    public Task<MealResponse> SaveEstimatedMeal(SaveEstimatedMealRequest request) =>
        Send<MealResponse>(HttpMethod.Post, "api/v1/meals/estimated", request);

    private static async Task<MealPhotoDraftResponse> ReadAnalysis(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return (await response.Content.ReadFromJsonAsync<MealPhotoDraftResponse>())!;
        var message = await response.Content.ReadFromJsonAsync<string>() ?? "Analiza AI nie powiodła się.";
        throw new HttpRequestException(message, null, response.StatusCode);
    }

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
