using System.Net;
using System.Net.Http.Json;
using FormaAI.Contracts.Users;

namespace FormaAI.Web.Services;

public sealed class AccountClient(HttpClient http)
{
    public async Task<CurrentUserResponse?> GetCurrentUser()
    {
        var response = await http.GetAsync("api/account/me");
        return response.StatusCode == HttpStatusCode.Unauthorized
            ? null
            : await Read<CurrentUserResponse>(response);
    }

    public Task<CurrentUserResponse> Register(RegisterRequest request) =>
        Send<CurrentUserResponse>(HttpMethod.Post, "api/account/register", request);

    public Task<CurrentUserResponse> Login(LoginRequest request) =>
        Send<CurrentUserResponse>(HttpMethod.Post, "api/account/login", request);

    public async Task Logout()
    {
        using var response = await Send(HttpMethod.Post, "api/account/logout", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAccount(DeleteAccountRequest request)
    {
        using var response = await Send(HttpMethod.Delete, "api/account", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<UserProfileResponse> GetProfile()
    {
        using var response = await http.GetAsync("api/profile");
        return await Read<UserProfileResponse>(response);
    }

    public Task<UserProfileResponse> UpdateProfile(UpdateUserProfileRequest request) =>
        Send<UserProfileResponse>(HttpMethod.Put, "api/profile", request);

    private async Task<T> Send<T>(HttpMethod method, string uri, object body)
    {
        using var response = await Send(method, uri, body);
        return await Read<T>(response);
    }

    private async Task<HttpResponseMessage> Send(HttpMethod method, string uri, object? body)
    {
        var csrf = await http.GetFromJsonAsync<AntiforgeryResponse>("api/account/antiforgery");
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await http.SendAsync(request);
    }

    private static async Task<T> Read<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
