using BlazorApp.Client;
using BlazorApp.Client.Auth;
using BlazorApp.Client.Common;
using BlazorApp.Client.Resources;
using BlazorApp.Client.Services;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Syncfusion.Blazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("ODQ2MzcxQDMyMzAyZTM0MmUzMEtIWTBrNnl5OEhLVFBkYVdOV1U3Zlk4OGZCd2ZBTVJtYVdlbStEbVhaUDQ9");

// Intelligent konfigurasjon basert på miljø
var apiPrefix = builder.Configuration["API_Prefix"];

// Hvis ikke satt i konfigurasjon, bruk miljø-basert fallback
if (string.IsNullOrEmpty(apiPrefix))
{
    apiPrefix = builder.HostEnvironment.IsDevelopment() 
        ? "http://localhost:7071/"  // Development
        : "";                      // Production
    }


Console.WriteLine($"🌐 Environment: {builder.HostEnvironment.Environment}");
Console.WriteLine($"🌐 IsDevelopment: {builder.HostEnvironment.IsDevelopment()}");
Console.WriteLine($"🌐 API_Prefix from config: {builder.Configuration["API_Prefix"]}");
Console.WriteLine($"🌐 Using API_Prefix: {apiPrefix}");

// HttpClient configuration
builder.Services.AddScoped(sp => 
{
    var baseAddress = !string.IsNullOrEmpty(apiPrefix) && apiPrefix.StartsWith("http") 
        ? apiPrefix 
        : builder.HostEnvironment.BaseAddress;
    Console.WriteLine($"🌐 HttpClient BaseAddress: {baseAddress}");
    return new HttpClient { BaseAddress = new Uri(baseAddress) };
});

builder.Services.AddSingleton<ISettings, Settings>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDataCacheService, DataCacheService>();
builder.Services.AddScoped<IBackgroundPreloadService, BackgroundPreloadService>();
builder.Services.AddAuthorizationCore(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, SwaAuthenticationStateProvider>();
builder.Services.AddSyncfusionBlazor();
builder.Services.AddLocalization(opts => opts.ResourcesPath = "Resources");
await builder.Build().RunAsync();
