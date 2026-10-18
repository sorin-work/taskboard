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

namespace Taskboard.Api.Features;

public static class EndpointRegistration
{
    /// <summary>Every feature's routes, so Program.cs doesn't grow a line per feature.</summary>
    public static IEndpointRouteBuilder MapTaskboardEndpoints(this IEndpointRouteBuilder app)
    {
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
        return app;
    }
}
