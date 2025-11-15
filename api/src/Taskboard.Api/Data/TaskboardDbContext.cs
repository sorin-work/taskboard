using Microsoft.EntityFrameworkCore;
using Taskboard.Api.Domain;

namespace Taskboard.Api.Data;

public class TaskboardDbContext(DbContextOptions<TaskboardDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskboardDbContext).Assembly);
    }
}
