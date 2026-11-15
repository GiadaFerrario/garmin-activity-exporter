using System.Text.Json.Serialization;
using ActivityExporter.Api;
using ActivityExporter.Core.Interfaces;
using ActivityExporter.Core.Models;
using ActivityExporter.Core.Service;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IActivityParser, FitActivityParser>();
builder.Services.AddSingleton<IActivityFormatter, FixedTextFormatter>();
builder.Services.AddScoped<ActivityImportService>();
// The history: a SQLite file (see ConnectionStrings in appsettings.json). The DbContext and the
// repository are scoped: one per web request, like the import service
builder.Services.AddDbContext<ActivityDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Activities")));
builder.Services.AddScoped<IActivityRepository, EfActivityRepository>();
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

app.Run();