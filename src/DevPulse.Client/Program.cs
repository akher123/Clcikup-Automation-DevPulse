using DevPulse.Client;
using DevPulse.Client.Services;
using DevPulse.Client.Services.Auth;
using DevPulse.Shared.Serialization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton(AppJsonOptions.Default);
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(DevPulse.Shared.Constants.AppRoles.Admin));
    options.AddPolicy("CanViewReports", policy =>
        policy.RequireRole(
            DevPulse.Shared.Constants.AppRoles.Admin,
            DevPulse.Shared.Constants.AppRoles.User));
});

builder.Services.AddScoped<CookieAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CookieAuthenticationStateProvider>());
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IClickUpAccountApiClient, ClickUpAccountApiClient>();
builder.Services.AddScoped<IDeveloperApiClient, DeveloperApiClient>();
builder.Services.AddScoped<IReportApiClient, ReportApiClient>();
builder.Services.AddScoped<IAnalyticsApiClient, AnalyticsApiClient>();
builder.Services.AddScoped<IUserApiClient, UserApiClient>();
builder.Services.AddScoped<IHolidayApiClient, HolidayApiClient>();
builder.Services.AddScoped<ILeaveApiClient, LeaveApiClient>();

await builder.Build().RunAsync();
