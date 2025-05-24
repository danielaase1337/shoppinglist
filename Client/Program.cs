using BlazorApp.Client;
using BlazorApp.Client.Common;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Syncfusion.Blazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("MjgyMjA4NUAzMjMzMmUzMDJlMzBESFk2eHhOd25SRXpraWpPRzUrZWUvSVhMam9IbzRrV3hYN3orQmpZMmpjPQ==");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.Configuration["API_Prefix"] ?? builder.HostEnvironment.BaseAddress) });

builder.Services.AddSingleton<ISettings, Settings>();
builder.Services.AddSyncfusionBlazor();

await builder.Build().RunAsync();
 