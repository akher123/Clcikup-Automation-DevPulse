using DevPulse.Application;
using DevPulse.Infrastructure;
using DevPulse.Shared.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => AppJsonOptions.Configure(options.JsonSerializerOptions));
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevPulseCors", policy =>
    {
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? ["https://localhost:7089", "http://localhost:5089"]);
    });
});

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("DevPulseCors");
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
