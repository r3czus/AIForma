using System.Net;
using System.Text;
using FormaAI.Infrastructure.External;

namespace FormaAI.Api.IntegrationTests;

public sealed class OpenFoodFactsClientTests
{
    [Fact]
    public async Task MapsNutrientsAndHandlesRateLimitWithoutRetrying()
    {
        var json = """
            {"code":"3017620422003","product":{"product_name":"Krem orzechowy","brands":"Marka","serving_quantity":15,"serving_quantity_unit":"g","nutriments":{"energy-kcal_100g":539,"proteins_100g":6.3,"fat_100g":30.9,"carbohydrates_100g":57.5}}}
            """;
        var handler = new StubHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        var client = new OpenFoodFactsClient(new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") });

        var result = await client.FindProduct("3017620422003");

        Assert.Equal(OpenFoodFactsResultStatus.Found, result.Status);
        Assert.Equal(539m, result.Product!.CaloriesPer100);
        Assert.Equal(1, handler.Calls);

        var limited = new OpenFoodFactsClient(new HttpClient(new StubHandler(new HttpResponseMessage(HttpStatusCode.TooManyRequests))) { BaseAddress = new Uri("https://example.test/") });
        Assert.Equal(OpenFoodFactsResultStatus.Unavailable, (await limited.FindProduct("3017620422003")).Status);
    }

    private sealed class StubHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(response);
        }
    }
}
