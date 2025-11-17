using Taskboard.Api.Common;
using Taskboard.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = ctx => ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddTaskboardDatabase(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

await app.MigrateDatabaseAsync();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

await app.RunAsync();

public partial class Program;
