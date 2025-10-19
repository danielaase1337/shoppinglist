using BlazorApp.Client;
using BlazorApp.Client.Common;
using BlazorApp.Client.Services;

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
builder.Services.AddScoped<IDataCacheService, DataCacheService>();
builder.Services.AddScoped<IBackgroundPreloadService, BackgroundPreloadService>();
builder.Services.AddSyncfusionBlazor();
await builder.Build().RunAsync();
