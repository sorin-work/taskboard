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

    public DbSet<BoardStar> BoardStars => Set<BoardStar>();

    public DbSet<BoardColumn> Columns => Set<BoardColumn>();

    public DbSet<Card> Cards => Set<Card>();

    public DbSet<Label> Labels => Set<Label>();

    public DbSet<CardLabel> CardLabels => Set<CardLabel>();

    public DbSet<CardAssignee> CardAssignees => Set<CardAssignee>();

    public DbSet<Checklist> Checklists => Set<Checklist>();

    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();

    public DbSet<Comment> Comments => Set<Comment>();

    public DbSet<Activity> Activities => Set<Activity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskboardDbContext).Assembly);
    }
}
