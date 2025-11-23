namespace Taskboard.Api.Features.Invitations;

public interface IInvitationNotifier
{
    Task SendAsync(string email, string workspaceName, string acceptUrl, CancellationToken ct);
}

/// <summary>
/// There is no mail server in this setup, so invitation links are written to
/// the log. Admins can also copy the link from the members page.
/// </summary>
public sealed partial class LoggingInvitationNotifier(ILogger<LoggingInvitationNotifier> logger) : IInvitationNotifier
{
    public Task SendAsync(string email, string workspaceName, string acceptUrl, CancellationToken ct)
    {
        LogInvitation(email, workspaceName, acceptUrl);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Invitation for {Email} to join {Workspace}: {AcceptUrl}")]
    private partial void LogInvitation(string email, string workspace, string acceptUrl);
}
