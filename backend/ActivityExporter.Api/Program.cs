using System.Text.Json.Serialization;
using ActivityExporter.Api;
using ActivityExporter.Core.Interfaces;
using ActivityExporter.Core.Models;
using ActivityExporter.Core.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IActivityParser, FitActivityParser>();
builder.Services.AddSingleton<IActivityFormatter, FixedTextFormatter>();
builder.Services.AddScoped<ActivityImportService>();
// The history: a SQLite file (see ConnectionStrings in appsettings.json). The DbContext and the
// repository are scoped: one per web request, like the import service
builder.Services.AddDbContext<ActivityDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Activities")));
builder.Services.AddScoped<IActivityRepository, EfActivityRepository>();

// The Personal API of Garmin. The key is a secret, it is not in appsettings.json:
//   dotnet user-secrets set "Garmin:ApiKey" "<the key>" --project ActivityExporter.Api
builder.Services.Configure<GarminApiOptions>(builder.Configuration.GetSection("Garmin"));
builder.Services.AddHttpClient("garmin", (services, client) =>
{
    client.BaseAddress = new Uri(services.GetRequiredService<IOptions<GarminApiOptions>>().Value.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddSingleton(TimeProvider.System);
// Singleton: the token is kept from one request to the next, until it is about to expire
builder.Services.AddSingleton(services => new GarminTokenProvider(
    services.GetRequiredService<IHttpClientFactory>().CreateClient("garmin"),
    services.GetRequiredService<IOptions<GarminApiOptions>>(),
    services.GetRequiredService<TimeProvider>()));
builder.Services.AddScoped<IGarminActivitySource>(services => new GarminApiClient(
    services.GetRequiredService<IHttpClientFactory>().CreateClient("garmin"),
    services.GetRequiredService<GarminTokenProvider>(),
    services.GetRequiredService<IOptions<GarminApiOptions>>()));
builder.Services.AddSingleton(new GarminSyncOptions());
builder.Services.AddScoped<GarminSyncService>();
builder.Services.AddScoped<ActivityHistoryService>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

// A local app with its own database file: it is created, or brought up to date, at every start
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<ActivityDbContext>().Database.Migrate();
}

app.MapActivityEndpoints();
app.MapGarminEndpoints();
app.MapHistoryEndpoints();

app.Run();