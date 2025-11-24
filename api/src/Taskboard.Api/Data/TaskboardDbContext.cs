using Microsoft.EntityFrameworkCore;
using Taskboard.Api.Domain;

namespace Taskboard.Api.Data;

public class TaskboardDbContext(DbContextOptions<TaskboardDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Workspace> Workspaces => Set<Workspace>();

    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();

    public DbSet<Invitation> Invitations => Set<Invitation>();

    public DbSet<Board> Boards => Set<Board>();

    public DbSet<BoardColumn> Columns => Set<BoardColumn>();

    public DbSet<Card> Cards => Set<Card>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskboardDbContext).Assembly);
    }
}
