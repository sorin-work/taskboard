using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Taskboard.Api.Auth;
using Taskboard.Api.Common;
using Taskboard.Api.Data;
using Taskboard.Api.Features.Activity;
using Taskboard.Api.Features.Auth;
using Taskboard.Api.Features.Boards;
using Taskboard.Api.Features.Cards;
using Taskboard.Api.Features.Checklists;
using Taskboard.Api.Features.Comments;
using Taskboard.Api.Features.Export;
using Taskboard.Api.Features.Health;
using Taskboard.Api.Features.Invitations;
using Taskboard.Api.Features.Labels;
using Taskboard.Api.Features.MyTasks;
using Taskboard.Api.Features.Search;
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
builder.Services.AddOpenApi();
builder.Services.AddScoped<WorkspaceAccess>();
builder.Services.AddScoped<ActivityLog>();
builder.Services.AddScoped<DemoSeeder>();
builder.Services.AddSingleton<IInvitationNotifier, LoggingInvitationNotifier>();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

var app = builder.Build();

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

app.MapHealthEndpoints();
app.MapAuthEndpoints();
app.MapWorkspaceEndpoints();
app.MapMemberEndpoints();
app.MapInvitationEndpoints();
app.MapBoardEndpoints();
app.MapColumnEndpoints();
app.MapCardEndpoints();
app.MapLabelEndpoints();
app.MapChecklistEndpoints();
app.MapCommentEndpoints();
app.MapActivityEndpoints();
app.MapMyTasksEndpoints();
app.MapSearchEndpoints();
app.MapExportEndpoints();

await app.RunAsync();

public partial class Program;
