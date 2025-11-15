using Taskboard.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTaskboardDatabase(builder.Configuration);

var app = builder.Build();

await app.MigrateDatabaseAsync();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

await app.RunAsync();

public partial class Program;
