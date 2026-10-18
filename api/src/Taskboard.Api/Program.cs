using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using Taskboard.Api.Auth;
using Taskboard.Api.Common;
using Taskboard.Api.Data;
using Taskboard.Api.Features;
using Taskboard.Api.Features.Activity;
using Taskboard.Api.Features.Invitations;
using Taskboard.Api.Features.Workspaces;
using Taskboard.Api.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = ctx => ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// Let bad request bodies reach ApiExceptionHandler instead of becoming a bare 400.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
builder.Services.AddTaskboardDatabase(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddTaskboardAuth(builder.Configuration);
builder.Services.AddTaskboardRateLimiting(builder.Configuration);

// Behind nginx every request comes from the proxy's address. Trust its
// X-Forwarded-For so rate limits and logs see the real client.
var trustForwardedHeaders = builder.Configuration.GetValue("ForwardedHeaders:Enabled", false);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddOpenApi();
builder.Services.AddScoped<WorkspaceAccess>();
builder.Services.AddScoped<ActivityLog>();
builder.Services.AddScoped<DemoSeeder>();
builder.Services.AddSingleton<IInvitationNotifier, LoggingInvitationNotifier>();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

var app = builder.Build();

if (trustForwardedHeaders)
{
    app.UseForwardedHeaders();
}

app.UseMiddleware<RequestIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

await app.MigrateDatabaseAsync();

if (app.Configuration.GetValue("SEED_DEMO", false))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<DemoSeeder>().SeedAsync();
}

if (app.Environment.IsDevelopment())
{
    // The api description, for tools like Bruno, Postman or an openapi client generator.
    app.MapOpenApi();
}

app.MapTaskboardEndpoints();

await app.RunAsync();

public partial class Program;
