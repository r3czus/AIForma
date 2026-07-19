using System.Text.Json;
using WebPush;

namespace FormaAI.Api.Services;

public sealed class VapidKeyStore
{
    private readonly VapidDetails _keys;

    public VapidKeyStore(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configuredPublic = configuration["WebPush:PublicKey"];
        var configuredPrivate = configuration["WebPush:PrivateKey"];
        if (!string.IsNullOrWhiteSpace(configuredPublic) && !string.IsNullOrWhiteSpace(configuredPrivate))
        {
            _keys = new VapidDetails("mailto:admin@formaai.local", configuredPublic, configuredPrivate);
            return;
        }

        var directory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "vapid-keys.json");
        if (File.Exists(path))
        {
            _keys = JsonSerializer.Deserialize<VapidDetails>(File.ReadAllText(path)) ?? throw new InvalidOperationException("Plik kluczy VAPID jest uszkodzony.");
            return;
        }

        _keys = VapidHelper.GenerateVapidKeys();
        File.WriteAllText(path, JsonSerializer.Serialize(_keys));
    }

    public string PublicKey => _keys.PublicKey;
    public VapidDetails Details => _keys;
}
