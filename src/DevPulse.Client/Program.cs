using DevPulse.Client;
using DevPulse.Client.Services;
using DevPulse.Shared.Serialization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton(AppJsonOptions.Default);
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<IClickUpAccountApiClient, ClickUpAccountApiClient>();
builder.Services.AddScoped<IDeveloperApiClient, DeveloperApiClient>();
builder.Services.AddScoped<IReportApiClient, ReportApiClient>();

await builder.Build().RunAsync();
