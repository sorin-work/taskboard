namespace Taskboard.Api.Domain;

/// <summary>Ordered from least to most privileged, so roles can be compared.</summary>
public enum WorkspaceRole
{
    Member = 0,
    Admin = 1,
    Owner = 2,
}
