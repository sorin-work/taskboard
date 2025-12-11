using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Taskboard.Api.Auth;
using Taskboard.Api.Common;
using Taskboard.Api.Data;
using Taskboard.Api.Features.Auth;
using Taskboard.Api.Features.Boards;
using Taskboard.Api.Features.Cards;
using Taskboard.Api.Features.Checklists;
using Taskboard.Api.Features.Comments;
using Taskboard.Api.Features.Invitations;
using Taskboard.Api.Features.Labels;
using Taskboard.Api.Features.Workspaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = ctx => ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddTaskboardDatabase(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddTaskboardAuth(builder.Configuration);
builder.Services.AddScoped<WorkspaceAccess>();
builder.Services.AddSingleton<IInvitationNotifier, LoggingInvitationNotifier>();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

await app.MigrateDatabaseAsync();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
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

await app.RunAsync();

public partial class Program;
