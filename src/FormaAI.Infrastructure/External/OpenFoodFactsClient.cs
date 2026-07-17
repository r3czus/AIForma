using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace FormaAI.Infrastructure.External;

public sealed class OpenFoodFactsClient(HttpClient http)
{
    public async Task<OpenFoodFactsResult> FindProduct(string barcode, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await http.GetAsync($"api/v3/product/{barcode}.json?fields=code,product_name,brands,nutriments,serving_quantity,serving_quantity_unit", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return OpenFoodFactsResult.NotFound();
            if (response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable) return OpenFoodFactsResult.Unavailable();
            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<OpenFoodFactsPayload>(cancellationToken: cancellationToken);
            var product = payload?.Product;
            if (product is null || string.IsNullOrWhiteSpace(product.Name)) return OpenFoodFactsResult.NotFound();
            return OpenFoodFactsResult.Found(new OpenFoodFactsProduct(
                payload!.Code ?? barcode,
                product.Name,
                product.Brands,
                product.Nutriments?.Calories ?? 0,
                product.Nutriments?.Protein ?? 0,
                product.Nutriments?.Fat ?? 0,
                product.Nutriments?.Carbohydrates ?? 0,
                product.ServingQuantity,
                product.ServingUnit));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return OpenFoodFactsResult.Unavailable(); }
        catch (HttpRequestException) { return OpenFoodFactsResult.Unavailable(); }
    }

    private sealed class OpenFoodFactsPayload
    {
        [JsonPropertyName("code")] public string? Code { get; init; }
        [JsonPropertyName("product")] public OpenFoodFactsProductPayload? Product { get; init; }
    }
    private sealed class OpenFoodFactsProductPayload
    {
        [JsonPropertyName("product_name")] public string? Name { get; init; }
        [JsonPropertyName("brands")] public string? Brands { get; init; }
        [JsonPropertyName("nutriments")] public NutrimentsPayload? Nutriments { get; init; }
        [JsonPropertyName("serving_quantity")] public decimal? ServingQuantity { get; init; }
        [JsonPropertyName("serving_quantity_unit")] public string? ServingUnit { get; init; }
    }
    private sealed class NutrimentsPayload
    {
        [JsonPropertyName("energy-kcal_100g")] public decimal? Calories { get; init; }
        [JsonPropertyName("proteins_100g")] public decimal? Protein { get; init; }
        [JsonPropertyName("fat_100g")] public decimal? Fat { get; init; }
        [JsonPropertyName("carbohydrates_100g")] public decimal? Carbohydrates { get; init; }
    }
}

public enum OpenFoodFactsResultStatus { Found, NotFound, Unavailable }
public sealed record OpenFoodFactsProduct(string Barcode, string Name, string? Brand, decimal CaloriesPer100, decimal ProteinPer100, decimal FatPer100, decimal CarbohydratesPer100, decimal? ServingQuantity, string? ServingUnit);
public sealed record OpenFoodFactsResult(OpenFoodFactsResultStatus Status, OpenFoodFactsProduct? Product)
{
    public static OpenFoodFactsResult Found(OpenFoodFactsProduct product) => new(OpenFoodFactsResultStatus.Found, product);
    public static OpenFoodFactsResult NotFound() => new(OpenFoodFactsResultStatus.NotFound, null);
    public static OpenFoodFactsResult Unavailable() => new(OpenFoodFactsResultStatus.Unavailable, null);
}
