using FormaAI.Infrastructure.Identity;
using FormaAI.Infrastructure.Persistence;
using FormaAI.Infrastructure.External;
using FormaAI.Application.Assistant;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using FormaAI.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("FormaAI");
var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(keysPath))
{
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
    if (OperatingSystem.IsWindows()) dataProtection.ProtectKeysWithDpapi();
}
var adminEmail = builder.Configuration["Admin:Email"];
builder.Services.AddAuthorization(options => options.AddPolicy("Admin", policy =>
    policy.RequireAssertion(context =>
        !string.IsNullOrWhiteSpace(adminEmail) &&
        string.Equals(context.User.Identity?.Name, adminEmail, StringComparison.OrdinalIgnoreCase))));
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "FormaAI.Antiforgery";
});
builder.Services.AddHealthChecks();
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("FormaAI")));
builder.Services.AddHttpClient<OpenFoodFactsClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["OpenFoodFacts:BaseUrl"] ?? "https://world.openfoodfacts.org/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd(builder.Configuration["OpenFoodFacts:UserAgent"] ?? "FormaAI/0.1 (local-development)");
    client.Timeout = TimeSpan.FromSeconds(6);
});
var gemini = new GeminiSettings(
    builder.Configuration["Gemini:ApiKey"] ?? string.Empty,
    builder.Configuration["Gemini:Model"] ?? "gemini-3.5-flash");
builder.Services.AddSingleton(gemini);
builder.Services.AddHttpClient<GeminiAssistantModel>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Gemini:BaseUrl"] ?? "https://generativelanguage.googleapis.com/");
    client.Timeout = TimeSpan.FromSeconds(45);
});
builder.Services.AddScoped<IAssistantModel>(services => services.GetRequiredService<GeminiAssistantModel>());
builder.Services.AddSingleton<VapidKeyStore>();
builder.Services.AddHostedService<ReminderWorker>();

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "FormaAI.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health/live").AllowAnonymous();
app.MapFallbackToFile("index.html");

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational()) await db.Database.MigrateAsync();
}

app.Run();

public partial class Program;
