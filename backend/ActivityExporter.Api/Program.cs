var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/api/ping", () => new { message = "pong" });

app.Run();