using Taskboard.Api.Auth;
using Taskboard.Api.Common;
using Taskboard.Api.Data;
using Taskboard.Api.Features.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = ctx => ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddTaskboardDatabase(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddTaskboardAuth(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

await app.MigrateDatabaseAsync();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
app.MapAuthEndpoints();

await app.RunAsync();

public partial class Program;
