using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using FormaAI.Web;
using FormaAI.Web.Services;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<AccountClient>();
builder.Services.AddScoped<NutritionClient>();
builder.Services.AddScoped<TrainingClient>();
builder.Services.AddScoped<ProgressClient>();
builder.Services.AddScoped<PantryClient>();
builder.Services.AddScoped<AssistantClient>();
builder.Services.AddScoped<AdminClient>();
builder.Services.AddMudServices();

await builder.Build().RunAsync();
