using System.Net.Http.Json;
using FormaAI.Contracts.Users;
using FormaAI.Contracts.Nutrition;
using FormaAI.Contracts.Assistant;
using FormaAI.Domain.Assistant;
using FormaAI.Domain.Nutrition;
using FormaAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace FormaAI.Api.IntegrationTests;

public sealed class UserDataIsolationTests : IClassFixture<FormaAiFactory>
{
    private readonly FormaAiFactory _factory;

    public UserDataIsolationTests(FormaAiFactory factory) => _factory = factory;

    [Fact]
    public async Task ProfileEndpointReturnsOnlySignedInUsersProfile()
    {
        var options = new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        };
        var anna = _factory.CreateClient(options);
        var jan = _factory.CreateClient(options);

        await Register(anna, "anna@example.test", "Europe/Warsaw");
        await Register(jan, "jan@example.test", "UTC");

        var annaProfile = await anna.GetFromJsonAsync<UserProfileResponse>("api/profile");
        var janProfile = await jan.GetFromJsonAsync<UserProfileResponse>("api/profile");

        Assert.Equal("anna@example.test", annaProfile!.Email);
        Assert.Equal("Europe/Warsaw", annaProfile.TimeZoneId);
        Assert.Equal("jan@example.test", janProfile!.Email);
        Assert.Equal("UTC", janProfile.TimeZoneId);
        Assert.NotEqual(annaProfile.Id, janProfile.Id);
    }

    [Fact]
    public async Task StrongAlphanumericPasswordCanBeUsedForAnAccount()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var csrf = await client.GetFromJsonAsync<AntiforgeryResponse>("api/account/antiforgery");
        var request = new HttpRequestMessage(HttpMethod.Post, "api/account/register")
        {
            Content = JsonContent.Create(new RegisterRequest("alphanumeric@example.test", "Dreczek123", "Europe/Warsaw"))
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var account = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();
        Assert.Equal("alphanumeric@example.test", account!.Email);
    }

    [Fact]
    public async Task AccountDeletionRemovesOwnedData()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        await Register(client, "delete-me@example.test", "UTC");
        var current = await client.GetFromJsonAsync<CurrentUserResponse>("api/account/me");
        var csrf = await client.GetFromJsonAsync<AntiforgeryResponse>("api/account/antiforgery");
        var productRequest = new HttpRequestMessage(HttpMethod.Post, "api/v1/products")
        {
            Content = JsonContent.Create(new SaveProductRequest("Produkt do usunięcia", null, 100, 10, 2, 5, null, ServingUnit.Gram))
        };
        productRequest.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        (await client.SendAsync(productRequest)).EnsureSuccessStatusCode();

        csrf = await client.GetFromJsonAsync<AntiforgeryResponse>("api/account/antiforgery");
        var delete = new HttpRequestMessage(HttpMethod.Delete, "api/account") { Content = JsonContent.Create(new DeleteAccountRequest("FormaAI!123", "USUŃ")) };
        delete.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        (await client.SendAsync(delete)).EnsureSuccessStatusCode();
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, (await client.GetAsync("api/account/me")).StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.UserProfiles.AnyAsync(x => x.UserId == current!.Id));
        Assert.False(await db.Products.AnyAsync(x => x.OwnerUserId == current!.Id));
    }

    [Fact]
    public async Task OnlyAdminCanConfigureAiAndApiKeyIsNotReturned()
    {
        var options = new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") };
        var admin = _factory.CreateClient(options);
        var other = _factory.CreateClient(options);
        await Register(admin, "admin@example.test", "UTC");
        await Register(other, "regular@example.test", "UTC");

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, (await other.GetAsync("api/v1/admin/ai")).StatusCode);
        var csrf = await admin.GetFromJsonAsync<AntiforgeryResponse>("api/account/antiforgery");
        var save = new HttpRequestMessage(HttpMethod.Put, "api/v1/admin/ai")
        {
            Content = JsonContent.Create(new SaveAiConfigurationRequest(AiProvider.Gemini, "https://generativelanguage.googleapis.com/", "gemini-test", "secret-test-key"))
        };
        save.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        var response = await admin.SendAsync(save);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("secret-test-key", json);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.AiConfigurations.SingleAsync();
        Assert.DoesNotContain("secret-test-key", stored.EncryptedApiKey);
    }

    private static async Task Register(HttpClient client, string email, string timeZone)
    {
        var csrf = await client.GetFromJsonAsync<AntiforgeryResponse>("api/account/antiforgery");
        var request = new HttpRequestMessage(HttpMethod.Post, "api/account/register")
        {
            Content = JsonContent.Create(new RegisterRequest(email, "FormaAI!123", timeZone))
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}

public class FormaAiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Admin:Email", "admin@example.test");
        builder.ConfigureLogging(logging => logging.ClearProviders().AddConsole());
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            var dbOptions = services
                .Where(x => x.ServiceType.Name == "IDbContextOptionsConfiguration`1")
                .ToList();
            foreach (var option in dbOptions)
            {
                services.Remove(option);
            }

            var dbName = $"formaai-tests-{Guid.NewGuid()}";
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(dbName));
        });
    }
}
