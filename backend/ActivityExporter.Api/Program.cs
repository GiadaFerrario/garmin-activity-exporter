using System.Text.Json.Serialization;
using ActivityExporter.Api;
using ActivityExporter.Core.Interfaces;
using ActivityExporter.Core.Models;
using ActivityExporter.Core.Service;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IActivityParser, FitActivityParser>();
builder.Services.AddSingleton<IActivityFormatter, FixedTextFormatter>();
builder.Services.AddScoped<ActivityImportService>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

app.MapActivityEndpoints();

app.Run();